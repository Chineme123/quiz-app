using System;

namespace AuthService.Application.DTOs
{
    public record RegisterRequest(string Email, string Password, string Role);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string Token, Guid UserId, string Role);
}
