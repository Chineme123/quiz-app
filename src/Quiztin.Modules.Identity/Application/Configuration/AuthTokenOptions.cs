using System;

namespace Quiztin.Modules.Identity.Application.Configuration
{
    /// <summary>Bound from the <c>AuthTokens</c> configuration section.</summary>
    public sealed class AuthTokenOptions
    {
        /// <summary>Access token lifetime. Short, because it is held in memory and replayable if stolen.</summary>
        public int AccessTokenMinutes { get; set; } = 15;

        public int RefreshTokenDays { get; set; } = 14;

        /// <summary>
        /// How long after a rotation a replay of the old token is still treated as a
        /// benign tab race rather than theft. Too long weakens reuse detection; too
        /// short signs users out when two tabs boot together.
        /// </summary>
        public int RotationGraceSeconds { get; set; } = 10;

        public RefreshCookieOptions Cookie { get; set; } = new();

        public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenMinutes);
        public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(RefreshTokenDays);
        public TimeSpan RotationGracePeriod => TimeSpan.FromSeconds(RotationGraceSeconds);
    }

    public sealed class RefreshCookieOptions
    {
        public string Name { get; set; } = "quiztin_rt";

        /// <summary>Scoped so the cookie never rides a profile or quiz request.</summary>
        public string Path { get; set; } = "/api/auth";

        /// <summary>False only for local http development. True everywhere else.</summary>
        public bool Secure { get; set; }
    }
}
