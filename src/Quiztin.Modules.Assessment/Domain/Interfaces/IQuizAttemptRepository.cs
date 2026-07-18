using System;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.Interfaces
{
    public interface IQuizAttemptRepository
    {
        Task AddAsync(QuizAttempt attempt);
        Task<QuizAttempt> GetByIdAsync(Guid id);
        Task UpdateAsync(QuizAttempt attempt);
        Task<QuizAttempt> FindByStudentAndQuizAsync(Guid studentId, Guid quizId);

        /// <summary>
        /// Attempts that actually spent one of the quiz's MaxAttempts (spec 0006, AC-15): the
        /// ones the student finished, plus any abandoned by starting over. The attempt they are
        /// currently on does not count, and neither does one they explicitly quit.
        /// </summary>
        Task<int> GetConsumedAttemptCountAsync(Guid studentId, Guid quizId);

        /// <summary>
        /// The student's running attempt on this quiz, if any. At most one exists: that is the
        /// one-active-attempt rule, kept true by trigger 4 abandoning a prior one at start.
        /// </summary>
        Task<QuizAttempt?> GetInProgressAttemptAsync(Guid studentId, Guid quizId);

        /// <summary>
        /// The student's attempts across the given quizzes, newest first, so the available list
        /// can show the right action per quiz without a query each (spec 0006, AC-2).
        /// </summary>
        Task<IReadOnlyList<QuizAttempt>> GetAttemptsForQuizzesAsync(Guid studentId, IReadOnlyList<Guid> quizIds);
        Task<bool> HasCommandBeenProcessedAsync(Guid commandId);
        Task MarkCommandAsProcessedAsync(Guid commandId, string type);
        
        // Transaction support — runs `operation` inside a retriable transaction.
        // The execution strategy + commit/rollback live in Infrastructure so the
        // Domain layer stays free of EF Core types (code-standards.md §3).
        Task ExecuteInTransactionAsync(Func<Task> operation);
    }
}
