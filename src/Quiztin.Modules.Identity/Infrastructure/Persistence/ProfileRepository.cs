using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Quiztin.Modules.Identity.Application.Interfaces;
using Quiztin.Modules.Identity.Domain.Entities;
using Quiztin.Modules.Identity.Infrastructure.Persistence;

namespace Quiztin.Modules.Identity.Infrastructure.Persistence
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly IdentityDbContext _context;

        public ProfileRepository(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task<Profile?> GetByUserIdAsync(Guid userId)
        {
            return await _context.Profiles
                                 .Include(p => p.User)
                                 .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task AddAsync(Profile profile)
        {
            await _context.Profiles.AddAsync(profile);
        }

        public async Task UpdateAsync(Profile profile)
        {
            _context.Profiles.Update(profile);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
