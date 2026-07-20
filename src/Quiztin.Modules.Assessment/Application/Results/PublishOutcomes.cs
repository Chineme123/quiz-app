using Quiztin.Modules.Assessment.Application.DTOs;

namespace Quiztin.Modules.Assessment.Application.Results
{
    /// <summary>
    /// Outcome of a publish or unpublish (spec 0009). The outcome enum pattern is used here (as
    /// in the classroom slice, spec 0008) because these endpoints span 400 and 404, and an enum
    /// maps each case to one status without exceptions carrying control flow.
    ///
    /// A quiz that does not exist and one owned by another teacher both report NotFound, so a
    /// quiz's existence never leaks across tenants (AC-1).
    /// </summary>
    public enum PublishOutcome
    {
        Ok,
        NotFound,
        NoQuestions,
        InvalidWindow,
        InvalidMaxAttempts
    }

    public class PublishResult
    {
        public PublishOutcome Outcome { get; set; }
        public QuizDto? Quiz { get; set; }

        public static PublishResult Ok(QuizDto quiz) => new() { Outcome = PublishOutcome.Ok, Quiz = quiz };
        public static PublishResult Failed(PublishOutcome outcome) => new() { Outcome = outcome };
    }
}
