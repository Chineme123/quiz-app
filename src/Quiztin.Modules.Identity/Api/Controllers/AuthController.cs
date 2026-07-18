using System;
using System.Threading.Tasks;
using Quiztin.Modules.Identity.Application.Configuration;
using Quiztin.Modules.Identity.Application.DTOs;
using Quiztin.Modules.Identity.Application.Interfaces;
using Quiztin.Modules.Identity.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Quiztin.Modules.Identity.Api.Controllers
{
    /// <summary>
    /// The controller owns every read and write of the refresh cookie, so the application
    /// layer never touches HttpContext and stays testable without a web host.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly IRefreshTokenService _refresh;
        private readonly AuthTokenOptions _options;

        public AuthController(IAuthService auth, IRefreshTokenService refresh, AuthTokenOptions options)
        {
            _auth = auth;
            _refresh = refresh;
            _options = options;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _auth.RegisterAsync(request);
                SetRefreshCookie(result.RefreshToken);
                return Ok(result.Response);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _auth.LoginAsync(request);
                SetRefreshCookie(result.RefreshToken);
                return Ok(result.Response);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { error = "Invalid email or password." });
            }
        }

        /// <summary>
        /// Exchanges the refresh cookie for a new access token. Authenticated by the cookie
        /// itself, so it carries no [Authorize]: the whole point is that the access token
        /// has already expired.
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                var outcome = await _refresh.RefreshAsync(ReadRefreshCookie());

                // Null means the token was rotated inside the grace window by another tab.
                // That tab's response already set the successor cookie; overwriting it here
                // with a stale value would break the session we are trying to save.
                if (outcome.RotatedRefreshToken is not null)
                    SetRefreshCookie(outcome.RotatedRefreshToken);

                return Ok(outcome.Response);
            }
            catch (UnauthorizedAccessException)
            {
                ClearRefreshCookie();
                return Unauthorized(new { error = "Your session has expired. Please sign in again." });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _refresh.LogoutAsync(ReadRefreshCookie());
            ClearRefreshCookie();
            return NoContent();
        }

        private string? ReadRefreshCookie() => Request.Cookies[_options.Cookie.Name];

        private void SetRefreshCookie(IssuedRefreshToken token) =>
            Response.Cookies.Append(_options.Cookie.Name, token.RawToken, BuildCookieOptions(token.ExpiresAt));

        private void ClearRefreshCookie() =>
            Response.Cookies.Delete(_options.Cookie.Name, BuildCookieOptions(expires: null));

        /// <summary>
        /// Host-only (no Domain), so the cookie survives the Vite dev proxy and, later,
        /// the gateway unchanged. Path-scoped so it never rides a profile or quiz request.
        /// </summary>
        private CookieOptions BuildCookieOptions(DateTimeOffset? expires) => new()
        {
            HttpOnly = true,
            Secure = _options.Cookie.Secure,
            SameSite = SameSiteMode.Lax,
            Path = _options.Cookie.Path,
            Expires = expires,
            IsEssential = true,
        };
    }
}
