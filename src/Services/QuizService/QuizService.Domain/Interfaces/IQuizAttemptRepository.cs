using System;
using System.Threading.Tasks;
using QuizService.Domain.Entities;

namespace QuizService.Domain.Interfaces
{
    public interface IQuizAttemptRepository
    {
        Task AddAsync(QuizAttempt attempt);
        Task<QuizAttempt> GetByIdAsync(Guid id);
        Task UpdateAsync(QuizAttempt attempt);
        Task<QuizAttempt> FindByStudentAndQuizAsync(Guid studentId, Guid quizId);
        Task<int> GetAttemptCountAsync(Guid studentId, Guid quizId);
        Task<bool> HasCommandBeenProcessedAsync(Guid commandId);
        Task MarkCommandAsProcessedAsync(Guid commandId, string type);
        
        // Transaction support — runs `operation` inside a retriable transaction.
        // The execution strategy + commit/rollback live in Infrastructure so the
        // Domain layer stays free of EF Core types (code-standards.md §3).
        Task ExecuteInTransactionAsync(Func<Task> operation);
    }
}
