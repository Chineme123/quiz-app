using System;
using System.Threading.Tasks;
using AuthService.Application.DTOs;
using AuthService.Application.Models;
using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request);
        Task<AuthResult> LoginAsync(LoginRequest request);
    }

    public interface ITokenService
    {
        string GenerateToken(AuthUser user);
    }

    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }

    public interface IRefreshTokenService
    {
        /// <summary>Mints the first refresh token of a new session family.</summary>
        Task<IssuedRefreshToken> IssueAsync(Guid userId, Guid sessionId);

        /// <summary>
        /// Exchanges a refresh token for a new access token, rotating the refresh token.
        /// Throws <see cref="UnauthorizedAccessException"/> when the token is unknown,
        /// expired, or a genuine replay (which also revokes the whole session family).
        /// </summary>
        Task<RefreshOutcome> RefreshAsync(string? rawToken);

        /// <summary>Revokes the presented token. Idempotent, and safe with a missing cookie.</summary>
        Task LogoutAsync(string? rawToken);
    }

    /// <summary>
    /// Creates and hashes refresh tokens. A refresh token is already high-entropy random,
    /// so a fast digest is correct here; a password hash would only add latency per request.
    /// </summary>
    public interface IRefreshTokenGenerator
    {
        string CreateRawToken();
        string Hash(string rawToken);
    }
}
