using System;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace UserService.Application.Interfaces
{
    public interface IProfileRepository
    {
        Task<Profile?> GetByUserIdAsync(Guid userId);
        Task AddAsync(Profile profile);
        Task UpdateAsync(Profile profile);
        Task SaveChangesAsync();
    }
}
