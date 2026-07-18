using System;
using System.Threading.Tasks;
using Quiztin.Modules.Identity.Domain.Entities;

namespace Quiztin.Modules.Identity.Application.Interfaces
{
    public interface IProfileRepository
    {
        Task<Profile?> GetByUserIdAsync(Guid userId);
        Task AddAsync(Profile profile);
        Task UpdateAsync(Profile profile);
        Task SaveChangesAsync();
    }
}
