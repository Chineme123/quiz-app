using System;
using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Interfaces;

namespace AuthService.Infrastructure.Security
{
    /// <summary>
    /// 32 bytes of cryptographic randomness, base64url encoded so it survives a cookie
    /// unescaped. Stored only as a SHA-256 hex digest: the raw token is already full
    /// entropy, so stretching it with PBKDF2 would buy nothing and cost a hash per request.
    /// </summary>
    public class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        private const int TokenBytes = 32;

        public string CreateRawToken()
        {
            Span<byte> bytes = stackalloc byte[TokenBytes];
            RandomNumberGenerator.Fill(bytes);

            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public string Hash(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                throw new ArgumentException("A raw token is required.", nameof(rawToken));

            var digest = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(digest).ToLowerInvariant();
        }
    }
}
