using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security
{
    /// <summary>
    /// Issues HS256 JWTs. The signing key + issuer/audience come from JwtSettings
    /// (env / user-secrets — never committed). NameIdentifier carries the canonical
    /// Guid user id that every other service reads for identity + tenancy.
    /// </summary>
    public class JwtTokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public JwtTokenService(IConfiguration config) => _config = config;

        public string GenerateToken(AuthUser user)
        {
            var jwt = _config.GetSection("JwtSettings");
            var secret = jwt["Secret"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("JwtSettings:Secret is not configured (set JwtSettings__Secret).");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
