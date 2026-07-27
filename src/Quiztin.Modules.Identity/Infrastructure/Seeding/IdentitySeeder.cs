using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quiztin.Modules.Identity.Application.Interfaces;
using Quiztin.Modules.Identity.Domain.Entities;
using Quiztin.Modules.Identity.Domain.Models;
using Quiztin.Modules.Identity.Infrastructure.Persistence;

namespace Quiztin.Modules.Identity.Infrastructure.Seeding;

/// <summary>
/// Seeds the demo teacher and students as real users, so a developer can log in and drive the core
/// loop. Their ids match the Assessment module's seeded classroom/quiz Guids, so the plain-Guid
/// cross-module references line up. Development only, idempotent.
/// </summary>
public static class IdentitySeeder
{
    // MUST match DataSeeder.SeedTeacherId / SeedStudentId / SeedStudent2Id / SeedStudent3Id in the
    // Assessment module. Cross-module references are plain Guids by design; these are the contract.
    public static readonly Guid SeedTeacherId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    public static readonly Guid SeedStudentId = Guid.Parse("22222222-0000-0000-0000-000000000002");
    public static readonly Guid SeedStudent2Id = Guid.Parse("22222222-0000-0000-0000-000000000005");
    public static readonly Guid SeedStudent3Id = Guid.Parse("22222222-0000-0000-0000-000000000006");

    public const string SeedTeacherEmail = "teacher@quiztin.dev";
    public const string SeedStudentEmail = "student@quiztin.dev";
    public const string SeedStudent2Email = "alex@quiztin.dev";
    public const string SeedStudent3Email = "jordan@quiztin.dev";
    public const string SeedPassword = "Password123!";

    public static async Task SeedDevelopmentAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IdentityDbContext>>();

        try
        {
            var seededAny = false;
            var hash = hasher.Hash(SeedPassword);

            // The teacher and the three demo students. Two of the students take the seed quiz and
            // one does not, so the teacher's results screens (spec 0010) have real rows to show.
            var users = new[]
            {
                (SeedTeacherId, SeedTeacherEmail, "Teacher"),
                (SeedStudentId, SeedStudentEmail, "Student"),
                (SeedStudent2Id, SeedStudent2Email, "Student"),
                (SeedStudent3Id, SeedStudent3Email, "Student"),
            };
            foreach (var (id, email, role) in users)
            {
                if (!await context.AuthUsers.AnyAsync(u => u.Id == id))
                {
                    context.AuthUsers.Add(AuthUser.CreateSeed(id, email, hash, role));
                    seededAny = true;
                }
            }

            // Display-name profiles for the students, so results rows read as names rather than
            // emails (spec 0010, AC-13 shows the name, falling back to email only when unset).
            var profiles = new[]
            {
                (SeedStudentId, "Sam Carter"),
                (SeedStudent2Id, "Alex Rivera"),
                (SeedStudent3Id, "Jordan Lee"),
            };
            foreach (var (id, displayName) in profiles)
            {
                if (!await context.Profiles.AnyAsync(p => p.UserId == id))
                {
                    var profile = new Profile(id);
                    profile.ApplyCoreUpdates(new ProfileUpdateRequest { DisplayName = displayName });
                    context.Profiles.Add(profile);
                    seededAny = true;
                }
            }

            if (seededAny)
            {
                await context.SaveChangesAsync();
                // Never log the credentials themselves (security.md §6); they are the constants
                // above for a developer to read from source.
                logger.LogInformation("Seeded the demo teacher and students (Development only).");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding identity users.");
        }
    }
}
