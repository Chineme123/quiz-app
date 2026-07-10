using System;

namespace AuthService.Domain.Entities
{
    /// <summary>
    /// A long-lived credential exchanged for short-lived access tokens.
    /// Only the SHA-256 <see cref="TokenHash"/> is persisted; the raw value exists
    /// once, in the response that sets the cookie, and is never stored or logged.
    /// </summary>
    /// <remarks>
    /// Tokens form a <see cref="SessionId"/> "family". Rotating a token revokes it and
    /// points it at its successor, so replaying a dead token proves either theft or a
    /// benign tab race — the two are told apart by how recently it was replaced.
    /// </remarks>
    public class RefreshToken
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid SessionId { get; private set; }
        public string TokenHash { get; private set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }
        public DateTimeOffset? RevokedAt { get; private set; }
        public Guid? ReplacedByTokenId { get; private set; }

        private RefreshToken() { } // EF Core

        public RefreshToken(Guid userId, Guid sessionId, string tokenHash, DateTimeOffset createdAt, DateTimeOffset expiresAt)
        {
            if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
            if (sessionId == Guid.Empty) throw new ArgumentException("SessionId is required.", nameof(sessionId));
            if (string.IsNullOrWhiteSpace(tokenHash)) throw new ArgumentException("TokenHash is required.", nameof(tokenHash));
            if (expiresAt <= createdAt) throw new ArgumentException("ExpiresAt must be after CreatedAt.", nameof(expiresAt));

            Id = Guid.NewGuid();
            UserId = userId;
            SessionId = sessionId;
            TokenHash = tokenHash;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
        }

        public bool IsRevoked => RevokedAt.HasValue;

        public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

        public bool IsLive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

        /// <summary>Revokes this token. Idempotent: the first revocation time stands.</summary>
        public void Revoke(DateTimeOffset now)
        {
            if (!IsRevoked) RevokedAt = now;
        }

        /// <summary>
        /// Rotates this token onto <paramref name="successor"/>: revoke it and record what replaced it.
        /// </summary>
        public void ReplaceWith(RefreshToken successor, DateTimeOffset now)
        {
            if (successor is null) throw new ArgumentNullException(nameof(successor));
            if (successor.SessionId != SessionId)
                throw new ArgumentException("A successor must stay in the same session family.", nameof(successor));
            if (IsRevoked)
                throw new InvalidOperationException("This refresh token has already been revoked.");

            RevokedAt = now;
            ReplacedByTokenId = successor.Id;
        }

        /// <summary>
        /// True when this token was rotated moments ago. Two browser tabs booting together
        /// both present the cookie, and the slower one arrives just after the faster one
        /// rotated it. That is not theft, so the family must survive.
        /// </summary>
        public bool WasReplacedWithin(TimeSpan grace, DateTimeOffset now)
            => ReplacedByTokenId.HasValue
               && RevokedAt.HasValue
               && now - RevokedAt.Value <= grace;
    }
}
