using System;
using System.Threading.Tasks;
using Quiztin.Modules.Identity.Domain.Entities;
using Quiztin.Modules.Identity.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Quiztin.Modules.Identity.Infrastructure.Persistence
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IdentityDbContext _db;

        public RefreshTokenRepository(IdentityDbContext db) => _db = db;

        public Task<RefreshToken?> GetByHashAsync(string tokenHash) =>
            _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        public async Task AddAsync(RefreshToken token)
        {
            await _db.RefreshTokens.AddAsync(token);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> TryRotateAsync(RefreshToken current, RefreshToken successor)
        {
            // One SaveChanges, so the revocation of `current` and the insert of `successor`
            // land in a single transaction. The xmin concurrency token makes a second
            // rotation of `current` fail instead of quietly duplicating the family.
            await _db.RefreshTokens.AddAsync(successor);

            try
            {
                await _db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                _db.Entry(successor).State = EntityState.Detached;
                await _db.Entry(current).ReloadAsync();
                return false;
            }
        }

        public async Task RevokeAsync(RefreshToken token, DateTimeOffset revokedAt)
        {
            token.Revoke(revokedAt);
            await _db.SaveChangesAsync();
        }

        public Task RevokeFamilyAsync(Guid sessionId, DateTimeOffset revokedAt) =>
            // A set-based update: it must not fail on the concurrency token, because this
            // runs precisely when tokens in the family are being written by someone else.
            _db.RefreshTokens
                .Where(t => t.SessionId == sessionId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, revokedAt));
    }
}
