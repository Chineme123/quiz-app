using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuizService.Domain.Entities;
using QuizService.Infrastructure.Persistence;

namespace QuizService.Infrastructure.Seeding
{
    public static class DataSeeder
    {
        // Fixed ID for the seed teacher to ensure consistency across restarts
        // In a real microservice with Auth, this ID should match the seeded user in AuthService.
        // For now, we assume this is the ID the developer will use in their JWT tokens.
        public const string SeedTeacherId = "teacher-1"; 
        
        public static async Task SeedDevelopmentDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QuizDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<QuizDbContext>>();

            try
            {
                 // ensure database is created/migrated
                 await context.Database.MigrateAsync();

                 // Check if the classroom exists
                 var classroomName = "Seed Classroom (Dev)";
                 var existingClassroom = await context.Classrooms
                     .FirstOrDefaultAsync(c => c.Name == classroomName && c.TeacherId == SeedTeacherId);

                 if (existingClassroom == null)
                 {
                     logger.LogInformation("Seeding Development Data: Creating Classroom '{ClassroomName}' for Teacher '{TeacherId}'", classroomName, SeedTeacherId);
                     
                     var classroom = new Classroom(SeedTeacherId, classroomName);
                     // If existing, we could optionally force the ID to a known GUID too, but letting it generate is fine 
                     // as long as we can find it by Name+Teacher.
                     
                     await context.Classrooms.AddAsync(classroom);
                     await context.SaveChangesAsync();
                     
                     logger.LogInformation("Seeding completed. Classroom ID: {ClassroomId}", classroom.Id);
                 }
                 else
                 {
                     logger.LogInformation("Seeding Development Data: Classroom '{ClassroomName}' | '{ClassroomId}' already exists. Skipping.", classroomName, existingClassroom.Id);
                 }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }
    }
}
