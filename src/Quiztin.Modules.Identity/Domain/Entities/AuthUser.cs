using System;

namespace Quiztin.Modules.Identity.Domain.Entities
{
    /// <summary>
    /// An authentication principal. The <see cref="Id"/> is the canonical user identity
    /// used across services (issued in the JWT NameIdentifier claim).
    /// </summary>
    public class AuthUser
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string Role { get; private set; } = string.Empty; // "Student" or "Teacher"

        private AuthUser() { } // EF Core

        public AuthUser(string email, string passwordHash, string role)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
            if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
            if (role != "Student" && role != "Teacher") throw new ArgumentException("Role must be 'Student' or 'Teacher'.", nameof(role));

            Id = Guid.NewGuid();
            Email = email.Trim().ToLowerInvariant();
            PasswordHash = passwordHash;
            Role = role;
        }
    }
}
