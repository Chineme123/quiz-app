using System;
using System.Threading.Tasks;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence
{
    public class AuthUserRepository : IAuthUserRepository
    {
        private readonly AuthDbContext _db;

        public AuthUserRepository(AuthDbContext db) => _db = db;

        public Task<AuthUser?> GetByEmailAsync(string email) =>
            _db.AuthUsers.FirstOrDefaultAsync(u => u.Email == email);

        public Task<AuthUser?> GetByIdAsync(Guid id) =>
            _db.AuthUsers.FirstOrDefaultAsync(u => u.Id == id);

        public async Task AddAsync(AuthUser user)
        {
            await _db.AuthUsers.AddAsync(user);
            await _db.SaveChangesAsync();
        }
    }
}
