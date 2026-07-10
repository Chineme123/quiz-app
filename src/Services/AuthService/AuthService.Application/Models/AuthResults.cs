using System;
using AuthService.Application.DTOs;

namespace AuthService.Application.Models
{
    /// <summary>
    /// A freshly minted refresh token on its way to the cookie writer. This is the only
    /// place the raw value ever exists. It is never persisted, logged, or serialized.
    /// </summary>
    public sealed record IssuedRefreshToken(string RawToken, DateTimeOffset ExpiresAt);

    /// <summary>The wire response plus the cookie the caller must set.</summary>
    public sealed record AuthResult(AuthResponse Response, IssuedRefreshToken RefreshToken);

    /// <summary>
    /// The outcome of a refresh. <see cref="RotatedRefreshToken"/> is null when the
    /// presented token had already been rotated inside the grace window: the browser
    /// holds the successor cookie already, so overwriting it would break the session.
    /// </summary>
    public sealed record RefreshOutcome(AuthResponse Response, IssuedRefreshToken? RotatedRefreshToken);
}
