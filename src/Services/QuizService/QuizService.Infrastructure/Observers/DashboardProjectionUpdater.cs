using System;
using System.Threading.Tasks;
using QuizService.Domain.Events;
using QuizService.Domain.Observers;

namespace QuizService.Infrastructure.Observers
{
    public class DashboardProjectionUpdater : QuizService.Domain.Observers.IObserver<QuizAttemptGradedEvent>
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
