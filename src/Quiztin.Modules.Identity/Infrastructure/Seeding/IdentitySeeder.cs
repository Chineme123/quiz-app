using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quiztin.Modules.Identity.Application.Interfaces;
using Quiztin.Modules.Identity.Domain.Entities;
using Quiztin.Modules.Identity.Infrastructure.Persistence;

namespace Quiztin.Modules.Identity.Infrastructure.Seeding;

/// <summary>
/// Seeds the demo teacher and student as real users, so a developer can log in and drive
/// the core loop. Their ids match the Assessment module's seeded classroom/quiz Guids, so
/// the plain-Guid cross-module references line up. Development only, idempotent.
/// </summary>
public static class IdentitySeeder
{
    // MUST match DataSeeder.SeedTeacherId / SeedStudentId in the Assessment module.
    // Cross-module references are plain Guids by design; these seed values are the contract.
    public static readonly Guid SeedTeacherId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    public static readonly Guid SeedStudentId = Guid.Parse("22222222-0000-0000-0000-000000000002");

    public const string SeedTeacherEmail = "teacher@quiztin.dev";
    public const string SeedStudentEmail = "student@quiztin.dev";
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

            if (!await context.AuthUsers.AnyAsync(u => u.Id == SeedTeacherId))
            {
                context.AuthUsers.Add(AuthUser.CreateSeed(SeedTeacherId, SeedTeacherEmail, hash, "Teacher"));
                seededAny = true;
            }
            if (!await context.AuthUsers.AnyAsync(u => u.Id == SeedStudentId))
            {
                context.AuthUsers.Add(AuthUser.CreateSeed(SeedStudentId, SeedStudentEmail, hash, "Student"));
                seededAny = true;
            }

            if (seededAny)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded dev users: {Teacher} (Teacher) and {Student} (Student), password '{Password}'.",
                    SeedTeacherEmail, SeedStudentEmail, SeedPassword);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding identity users.");
        }
    }
}
