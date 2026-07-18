using System;
using System.Threading.Tasks;
using Quiztin.Modules.Identity.Application.DTOs;
using Quiztin.Modules.Identity.Application.Interfaces;
using Quiztin.Modules.Identity.Application.Models;
using Quiztin.Modules.Identity.Domain.Entities;
using Quiztin.Modules.Identity.Domain.Interfaces;

namespace Quiztin.Modules.Identity.Application.Services
{
    public class AuthAppService : IAuthService
    {
        private readonly IAuthUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokens;
        private readonly IRefreshTokenService _refreshTokens;

        public AuthAppService(
            IAuthUserRepository users,
            IPasswordHasher hasher,
            ITokenService tokens,
            IRefreshTokenService refreshTokens)
        {
            _users = users;
            _hasher = hasher;
            _tokens = tokens;
            _refreshTokens = refreshTokens;
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (await _users.GetByEmailAsync(email) is not null)
                throw new InvalidOperationException("An account with this email already exists.");

            var user = new AuthUser(email, _hasher.Hash(request.Password), request.Role);
            await _users.AddAsync(user);
            return await StartSessionAsync(user);
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            var user = await _users.GetByEmailAsync(email);
            if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            return await StartSessionAsync(user);
        }

        /// <summary>Every sign in opens a fresh session family, so revoking one leaves the others alone.</summary>
        private async Task<AuthResult> StartSessionAsync(AuthUser user)
        {
            var refreshToken = await _refreshTokens.IssueAsync(user.Id, Guid.NewGuid());
            var response = new AuthResponse(_tokens.GenerateToken(user), user.Id, user.Role);
            return new AuthResult(response, refreshToken);
        }
    }
}
