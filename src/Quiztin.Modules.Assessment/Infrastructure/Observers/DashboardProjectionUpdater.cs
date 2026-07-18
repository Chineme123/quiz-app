using System;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Domain.Events;
using Quiztin.Modules.Assessment.Domain.Observers;

namespace Quiztin.Modules.Assessment.Infrastructure.Observers
{
    public class DashboardProjectionUpdater : Quiztin.Modules.Assessment.Domain.Observers.IObserver<QuizAttemptGradedEvent>
    {
        // In a real scenario, this would inject a repository or DB context for a read model
        // e.g., IDashboardRepository dashboardRepo;

        public Task UpdateAsync(QuizAttemptGradedEvent domainEvent)
        {
            // Simulate updating a dashboard projection
            // Console.WriteLine($"[Dashboard] Updating for student {domainEvent.StudentId}, Quiz {domainEvent.QuizId}, Score: {domainEvent.Score}");
            
            // This is where we would write to a specialized read-optimized table
            // await _dashboardRepo.UpdateStudentStatsAsync(domainEvent.StudentId, domainEvent.Score);
            
            return Task.CompletedTask;
        }
    }
}
