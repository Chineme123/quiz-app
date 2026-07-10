using System.Threading.Tasks;
using AuthService.Application.DTOs;
using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }

    public interface ITokenService
    {
        string GenerateToken(AuthUser user);
    }

    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }
}
