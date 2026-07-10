using System;
using System.Threading.Tasks;
using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByHashAsync(string tokenHash);

        Task AddAsync(RefreshToken token);

        /// <summary>
        /// Persists a rotation: the mutated <paramref name="current"/> and its new
        /// <paramref name="successor"/> are written together, so no two callers can
        /// rotate the same token.
        /// </summary>
        /// <returns>
        /// False when another request rotated <paramref name="current"/> first. The entity
        /// is reloaded from the database, so the caller can inspect what actually happened.
        /// </returns>
        Task<bool> TryRotateAsync(RefreshToken current, RefreshToken successor);

        Task RevokeAsync(RefreshToken token, DateTimeOffset revokedAt);

        /// <summary>Revokes every live token sharing <paramref name="sessionId"/>.</summary>
        Task RevokeFamilyAsync(Guid sessionId, DateTimeOffset revokedAt);
    }
}
