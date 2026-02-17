using System;

namespace UserService.Domain.Entities
{
    public class User
    {
        public Guid UserId { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string Role { get; private set; } // "Student" or "Teacher"
        
        // Navigation Property
        public Profile? Profile { get; private set; }

        private User() { } // EF Core constructor

        public User(Guid userId, string email, string passwordHash, string role)
        {
            if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email cannot be empty.", nameof(email));
            if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("PasswordHash cannot be empty.", nameof(passwordHash));
            if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("Role cannot be empty.", nameof(role));

            UserId = userId;
            Email = email;
            PasswordHash = passwordHash;
            Role = role;
        }

        public void SetProfile(Profile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            Profile = profile;
        }
    }
}
