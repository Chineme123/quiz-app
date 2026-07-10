using System.Threading.Tasks;
using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces
{
    public interface IAuthUserRepository
    {
        Task<AuthUser?> GetByEmailAsync(string email);
        Task AddAsync(AuthUser user);
    }
}
