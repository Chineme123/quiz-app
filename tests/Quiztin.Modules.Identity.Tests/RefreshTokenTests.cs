using System;
using Quiztin.Modules.Identity.Domain.Entities;

namespace Quiztin.Modules.Identity.Tests
{
    /// <summary>Entity-level rules for the rotation chain.</summary>
    public class RefreshTokenTests
    {
        private static readonly DateTimeOffset Now = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);

        private static RefreshToken Make(Guid? sessionId = null, string hash = "hash-a") =>
            new(Guid.NewGuid(), sessionId ?? Guid.NewGuid(), hash, Now, Now.AddDays(14));

        [Fact]
        public void NewToken_IsLive()
        {
            var token = Make();

            Assert.False(token.IsRevoked);
            Assert.False(token.IsExpired(Now));
            Assert.True(token.IsLive(Now));
        }

        [Fact]
        public void Token_IsExpired_AtExactlyExpiresAt()
        {
            var token = Make();

            Assert.True(token.IsExpired(token.ExpiresAt));
            Assert.False(token.IsExpired(token.ExpiresAt.AddTicks(-1)));
        }

        [Fact]
        public void ReplaceWith_RevokesAndRecordsSuccessor()
        {
            var session = Guid.NewGuid();
            var current = Make(session);
            var successor = Make(session, "hash-b");

            current.ReplaceWith(successor, Now);

            Assert.True(current.IsRevoked);
            Assert.Equal(Now, current.RevokedAt);
            Assert.Equal(successor.Id, current.ReplacedByTokenId);
        }

        [Fact]
        public void ReplaceWith_AcrossDifferentSessionFamilies_Throws()
        {
            var current = Make();
            var foreignSuccessor = Make();

            Assert.Throws<ArgumentException>(() => current.ReplaceWith(foreignSuccessor, Now));
        }

        [Fact]
        public void ReplaceWith_OnAnAlreadyRevokedToken_Throws()
        {
            var session = Guid.NewGuid();
            var current = Make(session);
            current.Revoke(Now);

            Assert.Throws<InvalidOperationException>(() => current.ReplaceWith(Make(session, "hash-b"), Now));
        }

        [Fact]
        public void Revoke_IsIdempotent_FirstTimeStands()
        {
            var token = Make();

            token.Revoke(Now);
            token.Revoke(Now.AddMinutes(5));

            Assert.Equal(Now, token.RevokedAt);
        }

        [Fact]
        public void WasReplacedWithin_IsTrue_JustAfterRotation()
        {
            var session = Guid.NewGuid();
            var current = Make(session);
            current.ReplaceWith(Make(session, "hash-b"), Now);

            Assert.True(current.WasReplacedWithin(TimeSpan.FromSeconds(10), Now.AddSeconds(2)));
        }

        [Fact]
        public void WasReplacedWithin_IsFalse_OnceTheGraceWindowCloses()
        {
            var session = Guid.NewGuid();
            var current = Make(session);
            current.ReplaceWith(Make(session, "hash-b"), Now);

            Assert.False(current.WasReplacedWithin(TimeSpan.FromSeconds(10), Now.AddSeconds(11)));
        }

        [Fact]
        public void WasReplacedWithin_IsFalse_ForATokenRevokedWithoutASuccessor()
        {
            // A logout revokes without rotating. Replaying it is never benign.
            var token = Make();
            token.Revoke(Now);

            Assert.False(token.WasReplacedWithin(TimeSpan.FromSeconds(10), Now));
        }

        [Fact]
        public void Constructor_RejectsAnExpiryThatIsNotInTheFuture()
        {
            Assert.Throws<ArgumentException>(() =>
                new RefreshToken(Guid.NewGuid(), Guid.NewGuid(), "hash", Now, Now));
        }
    }
}
