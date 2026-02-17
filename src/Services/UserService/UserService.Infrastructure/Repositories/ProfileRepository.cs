using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Infrastructure.Data;

namespace UserService.Infrastructure.Repositories
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly UserDbContext _context;

        public ProfileRepository(UserDbContext context)
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
