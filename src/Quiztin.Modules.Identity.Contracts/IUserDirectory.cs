using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quiztin.Modules.Identity.Contracts
{
    /// <summary>
    /// A user's public display identity, resolved across a module boundary: the display name
    /// (which may be null when the user has not completed a profile) and the email (always
    /// present). Nothing else crosses — no role, no academic data — because a name row needs
    /// nothing else.
    /// </summary>
    public sealed record UserIdentity(Guid UserId, string? DisplayName, string Email);

    /// <summary>
    /// The Identity module's published contract for turning a set of user ids into their
    /// display identity. Identity owns and implements it; other modules consume it in process.
    /// The first consumer is Assessment's classroom results (spec 0010, AC-13); spec 0008's
    /// deferred teacher-name lookup is the same contract, so it resolves any user, not only
    /// students.
    ///
    /// This is a directory lookup, not an authorization check: a caller passes only the ids its
    /// own tenant already holds (security.md §7), and the lookup returns names, it never widens
    /// access.
    /// </summary>
    public interface IUserDirectory
    {
        /// <summary>
        /// Resolves each id in <paramref name="userIds"/> that exists to its
        /// <see cref="UserIdentity"/>. Ids with no user are simply absent from the result: never
        /// a throw, so a caller can render a fallback rather than fail a whole page.
        /// </summary>
        Task<IReadOnlyDictionary<Guid, UserIdentity>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds);
    }
}
