using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Quiztin.Modules.Identity.Contracts;

namespace Quiztin.Modules.Identity.Infrastructure.Persistence
{
    /// <summary>
    /// The in-process implementation of the cross-module user directory (spec 0010). It reads
    /// the canonical AuthUser (email, always present) and left-joins the optional Profile
    /// (display name, absent until the user completes their profile), for the set of ids the
    /// calling module already holds. Read only: it resolves names, it authorizes nothing.
    /// </summary>
    public class UserDirectory : IUserDirectory
    {
        private readonly IdentityDbContext _context;

        public UserDirectory(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyDictionary<Guid, UserIdentity>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds)
        {
            if (userIds.Count == 0)
                return new Dictionary<Guid, UserIdentity>();

            // Distinct the ids so a caller passing duplicates cannot inflate the query and the
            // dictionary key stays unique. The Profile is a correlated left join, not an inner
            // one: a user who authenticated but never saved a profile still resolves, with a null
            // display name that the caller falls back from (AC-13).
            var ids = userIds.Distinct().ToList();

            var rows = await _context.AuthUsers
                .Where(u => ids.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    DisplayName = _context.Profiles
                        .Where(p => p.UserId == u.Id)
                        .Select(p => p.DisplayName)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return rows.ToDictionary(
                r => r.Id,
                r => new UserIdentity(
                    r.Id,
                    string.IsNullOrWhiteSpace(r.DisplayName) ? null : r.DisplayName,
                    r.Email));
        }
    }
}
