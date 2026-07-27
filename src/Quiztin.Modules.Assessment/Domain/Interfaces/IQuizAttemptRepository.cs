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
        /// The student's latest submitted attempt on this quiz (SubmittedAt set, greatest
        /// SubmittedAt), with its answers loaded, for the teacher's drill-down (spec 0010, AC-6,
        /// AC-7). Null when the student has no submitted attempt. Unlike GetInProgressAttemptAsync
        /// and FindByStudentAndQuizAsync (latest started, which may be an open attempt), this is
        /// the submitted result AC-7 says counts. Ownership is the caller's to check first.
        /// </summary>
        Task<QuizAttempt?> GetLatestSubmittedByStudentAndQuizAsync(Guid studentId, Guid quizId);

        /// <summary>
        /// The student's attempts across the given quizzes, newest first, so the available list
        /// can show the right action per quiz without a query each (spec 0006, AC-2).
        /// </summary>
        Task<IReadOnlyList<QuizAttempt>> GetAttemptsForQuizzesAsync(Guid studentId, IReadOnlyList<Guid> quizIds);

        /// <summary>
        /// Whether this quiz has any attempt at all (spec 0009, AC-9). One attempt in any state is
        /// enough: it means a student has already seen this question set, so authoring locks and
        /// the set can never change under them. A quiz wide check, unlike the student scoped counts
        /// above.
        /// </summary>
        Task<bool> HasAnyAttemptAsync(Guid quizId);

        /// <summary>
        /// How many attempts each of the given quizzes has, for a teacher's quiz list (spec 0009,
        /// AC-10). Batched so the list does not run a count per quiz. A quiz with no attempts is
        /// absent from the map, so callers default it to zero.
        /// </summary>
        Task<IReadOnlyDictionary<Guid, int>> GetAttemptCountsByQuizAsync(IReadOnlyList<Guid> quizIds);

        Task<bool> HasCommandBeenProcessedAsync(Guid commandId);
        Task MarkCommandAsProcessedAsync(Guid commandId, string type);
        
        // Transaction support — runs `operation` inside a retriable transaction.
        // The execution strategy + commit/rollback live in Infrastructure so the
        // Domain layer stays free of EF Core types (code-standards.md §3).
        Task ExecuteInTransactionAsync(Func<Task> operation);
    }
}
