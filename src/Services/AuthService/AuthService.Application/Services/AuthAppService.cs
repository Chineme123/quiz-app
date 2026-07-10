using System;
using System.Threading.Tasks;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services
{
    public class AuthAppService : IAuthService
    {
        private readonly IAuthUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokens;

        public AuthAppService(IAuthUserRepository users, IPasswordHasher hasher, ITokenService tokens)
        {
            _users = users;
            _hasher = hasher;
            _tokens = tokens;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (await _users.GetByEmailAsync(email) is not null)
                throw new InvalidOperationException("An account with this email already exists.");

            var user = new AuthUser(email, _hasher.Hash(request.Password), request.Role);
            await _users.AddAsync(user);
            return new AuthResponse(_tokens.GenerateToken(user), user.Id, user.Role);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            var user = await _users.GetByEmailAsync(email);
            if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            return new AuthResponse(_tokens.GenerateToken(user), user.Id, user.Role);
        }
    }
}
