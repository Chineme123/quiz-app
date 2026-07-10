using System;
using System.Threading.Tasks;
using AuthService.Application.Configuration;
using AuthService.Application.Interfaces;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Moq;

namespace AuthService.Tests
{
    /// <summary>
    /// Covers the rotation contract, and above all the two branches of a replayed token:
    /// a benign tab race must keep the session alive, a genuine replay must kill the family.
    /// </summary>
    public class RefreshTokenServiceTests
    {
        private static readonly DateTimeOffset Now = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);
        private const int GraceSeconds = 10;

        private readonly Mock<IRefreshTokenRepository> _tokens = new();
        private readonly Mock<IAuthUserRepository> _users = new();
        private readonly Mock<IRefreshTokenGenerator> _generator = new();
        private readonly Mock<ITokenService> _accessTokens = new();
        private readonly FixedClock _clock = new(Now);
        private readonly AuthUser _user = new("teacher@quiztin.test", "pw-hash", "Teacher");

        private readonly AuthTokenOptions _options = new()
        {
            AccessTokenMinutes = 15,
            RefreshTokenDays = 14,
            RotationGraceSeconds = GraceSeconds,
        };

        private int _rawCounter;

        public RefreshTokenServiceTests()
        {
            _generator.Setup(g => g.CreateRawToken()).Returns(() => $"raw-{++_rawCounter}");
            _generator.Setup(g => g.Hash(It.IsAny<string>())).Returns((string raw) => $"H:{raw}");
            _accessTokens.Setup(t => t.GenerateToken(It.IsAny<AuthUser>())).Returns("access-token");
            _users.Setup(u => u.GetByIdAsync(_user.Id)).ReturnsAsync(_user);
            _tokens.Setup(t => t.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
            _tokens.Setup(t => t.RevokeAsync(It.IsAny<RefreshToken>(), It.IsAny<DateTimeOffset>())).Returns(Task.CompletedTask);
            _tokens.Setup(t => t.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>())).Returns(Task.CompletedTask);
        }

        private RefreshTokenService Service() => new(
            _tokens.Object, _users.Object, _generator.Object, _accessTokens.Object, _options, _clock);

        private RefreshToken LiveToken(Guid session, string hash = "H:raw-0") =>
            new(_user.Id, session, hash, Now.AddDays(-1), Now.AddDays(13));

        /// <summary>A token already rotated onto a successor at <paramref name="rotatedAt"/>.</summary>
        private RefreshToken RotatedToken(Guid session, DateTimeOffset rotatedAt)
        {
            var token = LiveToken(session);
            var successor = new RefreshToken(_user.Id, session, "H:successor", rotatedAt, rotatedAt.AddDays(14));
            token.ReplaceWith(successor, rotatedAt);
            return token;
        }

        // --- issuing ---------------------------------------------------------

        [Fact]
        public async Task IssueAsync_StoresTheHash_NeverTheRawToken()
        {
            RefreshToken? stored = null;
            _tokens.Setup(t => t.AddAsync(It.IsAny<RefreshToken>()))
                   .Callback<RefreshToken>(t => stored = t)
                   .Returns(Task.CompletedTask);

            var issued = await Service().IssueAsync(_user.Id, Guid.NewGuid());

            Assert.Equal("raw-1", issued.RawToken);
            Assert.NotNull(stored);
            Assert.Equal("H:raw-1", stored!.TokenHash);
            Assert.NotEqual(issued.RawToken, stored.TokenHash);
        }

        [Fact]
        public async Task IssueAsync_SetsExpiryFromConfiguredLifetime()
        {
            RefreshToken? stored = null;
            _tokens.Setup(t => t.AddAsync(It.IsAny<RefreshToken>()))
                   .Callback<RefreshToken>(t => stored = t)
                   .Returns(Task.CompletedTask);

            var issued = await Service().IssueAsync(_user.Id, Guid.NewGuid());

            Assert.Equal(Now.AddDays(14), issued.ExpiresAt);
            Assert.Equal(Now.AddDays(14), stored!.ExpiresAt);
        }

        // --- refreshing ------------------------------------------------------

        [Fact]
        public async Task RefreshAsync_WithNoCookie_Throws()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service().RefreshAsync(null));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service().RefreshAsync("   "));
        }

        [Fact]
        public async Task RefreshAsync_WithAnUnknownToken_Throws_AndRevokesNothing()
        {
            _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service().RefreshAsync("bogus"));

            _tokens.Verify(t => t.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()), Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_WithAnExpiredToken_Throws_AndRevokesNothing()
        {
            var session = Guid.NewGuid();
            var expired = new RefreshToken(_user.Id, session, "H:old", Now.AddDays(-30), Now.AddDays(-1));
            _tokens.Setup(t => t.GetByHashAsync("H:old-raw")).ReturnsAsync(expired);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service().RefreshAsync("old-raw"));

            _tokens.Verify(t => t.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()), Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_WithALiveToken_RotatesOntoANewToken()
        {
            var session = Guid.NewGuid();
            var current = LiveToken(session);
            _tokens.Setup(t => t.GetByHashAsync("H:current")).ReturnsAsync(current);
            _tokens.Setup(t => t.TryRotateAsync(current, It.IsAny<RefreshToken>())).ReturnsAsync(true);

            var outcome = await Service().RefreshAsync("current");

            Assert.Equal("access-token", outcome.Response.Token);
            Assert.Equal(_user.Id, outcome.Response.UserId);
            Assert.NotNull(outcome.RotatedRefreshToken);
            Assert.Equal("raw-1", outcome.RotatedRefreshToken!.RawToken);

            // The presented token is dead and points at what replaced it.
            Assert.True(current.IsRevoked);
            Assert.NotNull(current.ReplacedByTokenId);
        }

        [Fact]
        public async Task RefreshAsync_RotatesWithinTheSameSessionFamily()
        {
            var session = Guid.NewGuid();
            var current = LiveToken(session);
            RefreshToken? successor = null;
            _tokens.Setup(t => t.GetByHashAsync("H:current")).ReturnsAsync(current);
            _tokens.Setup(t => t.TryRotateAsync(current, It.IsAny<RefreshToken>()))
                   .Callback<RefreshToken, RefreshToken>((_, s) => successor = s)
                   .ReturnsAsync(true);

            await Service().RefreshAsync("current");

            Assert.Equal(session, successor!.SessionId);
            Assert.Equal("H:raw-1", successor.TokenHash);
        }

        [Fact]
        public async Task RefreshAsync_ReplayInsideTheGraceWindow_SucceedsWithoutRotating()
        {
            // The tab race: another tab rotated this token two seconds ago. The browser
            // already holds the successor cookie, so we must not rotate or re-set it.
            var session = Guid.NewGuid();
            var rotated = RotatedToken(session, Now.AddSeconds(-2));
            _tokens.Setup(t => t.GetByHashAsync("H:stale")).ReturnsAsync(rotated);

            var outcome = await Service().RefreshAsync("stale");

            Assert.Equal("access-token", outcome.Response.Token);
            Assert.Null(outcome.RotatedRefreshToken);

            _tokens.Verify(t => t.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()), Times.Never);
            _tokens.Verify(t => t.TryRotateAsync(It.IsAny<RefreshToken>(), It.IsAny<RefreshToken>()), Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_ReplayOnTheGraceBoundary_IsStillBenign()
        {
            var session = Guid.NewGuid();
            var rotated = RotatedToken(session, Now.AddSeconds(-GraceSeconds));
            _tokens.Setup(t => t.GetByHashAsync("H:stale")).ReturnsAsync(rotated);

            var outcome = await Service().RefreshAsync("stale");

            Assert.Null(outcome.RotatedRefreshToken);
            _tokens.Verify(t => t.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()), Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_ReplayOutsideTheGraceWindow_RevokesTheWholeFamily()
        {
            var session = Guid.NewGuid();
            var rotated = RotatedToken(session, Now.AddSeconds(-(GraceSeconds + 1)));
            _tokens.Setup(t => t.GetByHashAsync("H:stolen")).ReturnsAsync(rotated);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service().RefreshAsync("stolen"));

            _tokens.Verify(t => t.RevokeFamilyAsync(session, Now), Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_ReplayOfALoggedOutToken_RevokesTheFamily()
        {
            // Revoked with no successor: a logout, never a rotation race.
            var session = Guid.NewGuid();
            var loggedOut = LiveToken(session);
            loggedOut.Revoke(Now);
            _tokens.Setup(t => t.GetByHashAsync("H:dead")).ReturnsAsync(loggedOut);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service().RefreshAsync("dead"));

            _tokens.Verify(t => t.RevokeFamilyAsync(session, Now), Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_LosingTheRotationRace_IsTreatedAsABenignReplay()
        {
            // Two requests rotated the same token. The database rejected ours, so the
            // other one won moments ago. Do not sign the user out.
            var session = Guid.NewGuid();
            var current = LiveToken(session);
            _tokens.Setup(t => t.GetByHashAsync("H:current")).ReturnsAsync(current);
            _tokens.Setup(t => t.TryRotateAsync(current, It.IsAny<RefreshToken>())).ReturnsAsync(false);

            var outcome = await Service().RefreshAsync("current");

            Assert.Equal("access-token", outcome.Response.Token);
            Assert.Null(outcome.RotatedRefreshToken);
            _tokens.Verify(t => t.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()), Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_WhenTheAccountIsGone_Throws()
        {
            var session = Guid.NewGuid();
            _tokens.Setup(t => t.GetByHashAsync("H:current")).ReturnsAsync(LiveToken(session));
            _users.Setup(u => u.GetByIdAsync(_user.Id)).ReturnsAsync((AuthUser?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service().RefreshAsync("current"));
        }

        // --- logging out -----------------------------------------------------

        [Fact]
        public async Task LogoutAsync_RevokesALiveToken()
        {
            var token = LiveToken(Guid.NewGuid());
            _tokens.Setup(t => t.GetByHashAsync("H:current")).ReturnsAsync(token);

            await Service().LogoutAsync("current");

            _tokens.Verify(t => t.RevokeAsync(token, Now), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_WithoutACookie_DoesNothing()
        {
            await Service().LogoutAsync(null);

            _tokens.Verify(t => t.GetByHashAsync(It.IsAny<string>()), Times.Never);
            _tokens.Verify(t => t.RevokeAsync(It.IsAny<RefreshToken>(), It.IsAny<DateTimeOffset>()), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_IsIdempotent_OnAnAlreadyRevokedToken()
        {
            var token = LiveToken(Guid.NewGuid());
            token.Revoke(Now.AddMinutes(-1));
            _tokens.Setup(t => t.GetByHashAsync("H:current")).ReturnsAsync(token);

            await Service().LogoutAsync("current");

            _tokens.Verify(t => t.RevokeAsync(It.IsAny<RefreshToken>(), It.IsAny<DateTimeOffset>()), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_WithAnUnknownToken_DoesNothing()
        {
            _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken?)null);

            await Service().LogoutAsync("bogus");

            _tokens.Verify(t => t.RevokeAsync(It.IsAny<RefreshToken>(), It.IsAny<DateTimeOffset>()), Times.Never);
        }

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }
    }
}
