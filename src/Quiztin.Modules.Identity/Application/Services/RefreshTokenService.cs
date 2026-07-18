using System;
using System.Threading.Tasks;
using Quiztin.Modules.Identity.Application.Configuration;
using Quiztin.Modules.Identity.Application.DTOs;
using Quiztin.Modules.Identity.Application.Interfaces;
using Quiztin.Modules.Identity.Application.Models;
using Quiztin.Modules.Identity.Domain.Entities;
using Quiztin.Modules.Identity.Domain.Interfaces;

namespace Quiztin.Modules.Identity.Application.Services
{
    /// <summary>
    /// Owns the refresh token lifecycle: issue, rotate, detect reuse, revoke.
    /// </summary>
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _tokens;
        private readonly IAuthUserRepository _users;
        private readonly IRefreshTokenGenerator _generator;
        private readonly ITokenService _accessTokens;
        private readonly AuthTokenOptions _options;
        private readonly TimeProvider _clock;

        public RefreshTokenService(
            IRefreshTokenRepository tokens,
            IAuthUserRepository users,
            IRefreshTokenGenerator generator,
            ITokenService accessTokens,
            AuthTokenOptions options,
            TimeProvider clock)
        {
            _tokens = tokens;
            _users = users;
            _generator = generator;
            _accessTokens = accessTokens;
            _options = options;
            _clock = clock;
        }

        public async Task<IssuedRefreshToken> IssueAsync(Guid userId, Guid sessionId)
        {
            var now = _clock.GetUtcNow();
            var raw = _generator.CreateRawToken();

            var token = new RefreshToken(
                userId,
                sessionId,
                _generator.Hash(raw),
                now,
                now.Add(_options.RefreshTokenLifetime));

            await _tokens.AddAsync(token);
            return new IssuedRefreshToken(raw, token.ExpiresAt);
        }

        public async Task<RefreshOutcome> RefreshAsync(string? rawToken)
        {
            var now = _clock.GetUtcNow();
            var token = await FindAsync(rawToken);

            if (token.IsRevoked)
                return await HandleReplayAsync(token, now);

            if (token.IsExpired(now))
                throw new UnauthorizedAccessException("Refresh token has expired.");

            var user = await LoadUserAsync(token.UserId);

            var successorRaw = _generator.CreateRawToken();
            var successor = new RefreshToken(
                token.UserId,
                token.SessionId,
                _generator.Hash(successorRaw),
                now,
                now.Add(_options.RefreshTokenLifetime));

            token.ReplaceWith(successor, now);

            if (!await _tokens.TryRotateAsync(token, successor))
            {
                // Another request rotated this exact token while we were working. The
                // repository reloaded it, so the same grace rule that covers a racing
                // tab covers us: recent means benign, stale means stolen.
                return await HandleReplayAsync(token, now);
            }

            return new RefreshOutcome(
                Respond(user),
                new IssuedRefreshToken(successorRaw, successor.ExpiresAt));
        }

        public async Task LogoutAsync(string? rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken)) return;

            var token = await _tokens.GetByHashAsync(_generator.Hash(rawToken));
            if (token is null || token.IsRevoked) return;

            await _tokens.RevokeAsync(token, _clock.GetUtcNow());
        }

        /// <summary>
        /// A revoked token was presented. If it was rotated moments ago this is a tab race:
        /// the browser already holds the successor cookie, so issue an access token and leave
        /// the cookie alone. Otherwise treat it as a stolen token and kill the whole family.
        /// </summary>
        private async Task<RefreshOutcome> HandleReplayAsync(RefreshToken token, DateTimeOffset now)
        {
            if (token.WasReplacedWithin(_options.RotationGracePeriod, now))
            {
                var user = await LoadUserAsync(token.UserId);
                return new RefreshOutcome(Respond(user), RotatedRefreshToken: null);
            }

            await _tokens.RevokeFamilyAsync(token.SessionId, now);
            throw new UnauthorizedAccessException("Refresh token has already been used.");
        }

        private async Task<RefreshToken> FindAsync(string? rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                throw new UnauthorizedAccessException("No refresh token was presented.");

            return await _tokens.GetByHashAsync(_generator.Hash(rawToken))
                   ?? throw new UnauthorizedAccessException("Refresh token is not recognised.");
        }

        private async Task<AuthUser> LoadUserAsync(Guid userId) =>
            await _users.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("The account for this token no longer exists.");

        private AuthResponse Respond(AuthUser user) =>
            new(_accessTokens.GenerateToken(user), user.Id, user.Role);
    }
}
