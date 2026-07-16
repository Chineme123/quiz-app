using QuizService.Application.DTOs;

namespace QuizService.Application.Results
{
    /// <summary>
    /// What happened when a student submitted an attempt (spec 0006, AC-11, AC-16).
    /// An explicit outcome rather than an exception: the controller maps it to a status
    /// code with no try/catch sprawl, and each path is directly testable (code-standards §8).
    /// </summary>
    public enum SubmitQuizOutcome
    {
        /// <summary>Graded, or an idempotent replay of an already graded attempt.</summary>
        Graded,

        /// <summary>Missing, or not the caller's. The controller answers 404 either way, so a
        /// non owner never learns the attempt exists (security.md §7).</summary>
        NotFound,

        /// <summary>Abandoned because a newer attempt superseded it (foundation §69 trigger 4).
        /// A stale tab auto submitting lands here instead of throwing out of the domain.</summary>
        Superseded
    }

    /// <summary>The outcome of a submit, plus the graded result when there is one.</summary>
    public sealed class SubmitQuizResult
    {
        public SubmitQuizOutcome Outcome { get; }
        public AttemptResultDto? Result { get; }

        private SubmitQuizResult(SubmitQuizOutcome outcome, AttemptResultDto? result)
        {
            Outcome = outcome;
            Result = result;
        }

        public static SubmitQuizResult Ok(AttemptResultDto result) => new(SubmitQuizOutcome.Graded, result);
        public static SubmitQuizResult NotFound() => new(SubmitQuizOutcome.NotFound, null);
        public static SubmitQuizResult Superseded() => new(SubmitQuizOutcome.Superseded, null);
    }

    /// <summary>
    /// What happened when a draft save was attempted (spec 0006, AC-6, AC-7).
    /// </summary>
    public enum SaveDraftOutcome
    {
        /// <summary>Saved. The whole draft set was replaced.</summary>
        Saved,

        /// <summary>Missing, or not the caller's. Answered as 404, same as every other
        /// attempt scoped endpoint.</summary>
        NotFound,

        /// <summary>Refused: the attempt is no longer running, or its time is up. This is
        /// what makes the deadline server enforced rather than merely client counted.</summary>
        Rejected
    }
}
