using System;
using System.Threading.Tasks;
using Quiztin.Modules.Identity.Domain.Entities;

namespace Quiztin.Modules.Identity.Domain.Interfaces
{
    public interface IAuthUserRepository
    {
        Task<AuthUser?> GetByEmailAsync(string email);
        Task<AuthUser?> GetByIdAsync(Guid id);
        Task AddAsync(AuthUser user);
    }
}
