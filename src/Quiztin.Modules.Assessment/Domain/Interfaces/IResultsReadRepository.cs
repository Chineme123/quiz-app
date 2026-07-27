using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quiztin.Modules.Assessment.Domain.Interfaces
{
    /// <summary>
    /// The read side for teacher classroom results (spec 0010). Every method is a plain read
    /// over the existing attempt tables — no writes, no new entities — returning lightweight
    /// projection rows the application layer totals. The tenancy scope is the ids the caller
    /// passes (the classroom's own quizzes and roster), which the application layer derives from
    /// the classroom the authenticated teacher owns (security.md §4, §7).
    /// </summary>
    public interface IResultsReadRepository
    {
        /// <summary>The currently enrolled students of a classroom, ordered stably for paging.</summary>
        Task<IReadOnlyList<EnrolledStudent>> GetEnrolledStudentsAsync(Guid classroomId);

        /// <summary>Which of these quiz ids have at least one attempt in any state (the summary's
        /// "published or attempted" filter).</summary>
        Task<IReadOnlyList<Guid>> GetQuizIdsWithAnyAttemptAsync(IReadOnlyList<Guid> quizIds);

        /// <summary>
        /// Every submitted attempt (SubmittedAt set) for these quizzes by these students. The
        /// caller reduces to the latest per (student, quiz); an attempt counts as submitted by its
        /// SubmittedAt, never by a state name, because grading moves an attempt through several
        /// states within seconds of submitting (index.md invariants).
        /// </summary>
        Task<IReadOnlyList<SubmittedAttemptRow>> GetSubmittedAttemptsAsync(
            IReadOnlyList<Guid> quizIds, IReadOnlyList<Guid> studentIds);

        /// <summary>
        /// The (student, quiz) pairs among these with an open attempt: started, not submitted, not
        /// abandoned. This is the "In progress" set (AC-8); a student shows In progress only when
        /// they have an open attempt and no submitted one, so the caller checks this after the
        /// submitted set.
        /// </summary>
        Task<IReadOnlyList<OpenAttemptRow>> GetOpenAttemptsAsync(
            IReadOnlyList<Guid> quizIds, IReadOnlyList<Guid> studentIds);

        /// <summary>
        /// Per-question correctness for a set of attempts (one row per answered question). Grading
        /// writes a QuizAnswer row for every question of a submitted attempt, including skipped
        /// ones (blank, marked incorrect), so counting IsCorrect over an attempt set gives the
        /// per-question fraction correct directly (AC-4).
        /// </summary>
        Task<IReadOnlyList<AnswerCorrectnessRow>> GetAnswerCorrectnessAsync(IReadOnlyList<Guid> attemptIds);
    }

    /// <summary>A roster entry: the student id and when they enrolled (the stable sort key).</summary>
    public sealed record EnrolledStudent(Guid StudentId, DateTime EnrolledAt);

    /// <summary>One submitted attempt, projected. Score is the attempt's total (nullable only for
    /// the theoretical window before synchronous grading writes it); SubmittedAt is the stamp the
    /// "latest submitted" rule sorts on.</summary>
    public sealed record SubmittedAttemptRow(
        Guid StudentId, Guid QuizId, Guid AttemptId, decimal? Score, DateTime? SubmittedAt);

    /// <summary>A (student, quiz) pair that has an open (in progress) attempt.</summary>
    public sealed record OpenAttemptRow(Guid StudentId, Guid QuizId);

    /// <summary>One answered question on one attempt, with whether it was correct.</summary>
    public sealed record AnswerCorrectnessRow(Guid QuestionId, bool IsCorrect);
}
