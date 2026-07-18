using System;
using System.Threading.Tasks;
using Quiztin.Modules.Identity.Domain.Entities;
using Quiztin.Modules.Identity.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Quiztin.Modules.Identity.Infrastructure.Persistence
{
    public class AuthUserRepository : IAuthUserRepository
    {
        private readonly IdentityDbContext _db;

        public AuthUserRepository(IdentityDbContext db) => _db = db;

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
