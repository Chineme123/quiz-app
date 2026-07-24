using System;
using System.Collections.Generic;
using Quiztin.Modules.Assessment.Application.DTOs;

namespace Quiztin.Modules.Assessment.Application.Results
{
    /// <summary>
    /// Outcome of an authoring write: create a quiz, or add, edit, or delete a question
    /// (spec 0009). As in the publish and classroom slices it uses an outcome enum rather than
    /// exceptions, because these endpoints span 200, 400, 404, and 409, and an enum maps each
    /// case to one status without exceptions carrying control flow.
    ///
    /// A quiz or question that does not exist and one owned by another teacher both report
    /// NotFound, so a quiz's existence never leaks across tenants (AC-3, AC-10). Locked is the
    /// 409 a quiz returns once it has an attempt: its question set is frozen so a student is
    /// never graded against a set that changed under them (AC-9).
    /// </summary>
    public enum AuthoringOutcome
    {
        Ok,
        NotFound,
        Invalid,
        Locked
    }

    public class AuthoringResult
    {
        public AuthoringOutcome Outcome { get; set; }
        public QuizDto? Quiz { get; set; }
        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();

        public static AuthoringResult Ok(QuizDto quiz) => new() { Outcome = AuthoringOutcome.Ok, Quiz = quiz };

        // Ok with no body, for a delete: the endpoint answers 204, so it needs no quiz back.
        public static AuthoringResult Deleted() => new() { Outcome = AuthoringOutcome.Ok };

        public static AuthoringResult NotFound() => new() { Outcome = AuthoringOutcome.NotFound };
        public static AuthoringResult Invalid(IReadOnlyList<string> errors) => new() { Outcome = AuthoringOutcome.Invalid, Errors = errors };
        public static AuthoringResult Invalid(string error) => new() { Outcome = AuthoringOutcome.Invalid, Errors = new[] { error } };
        public static AuthoringResult Locked() => new() { Outcome = AuthoringOutcome.Locked };
    }
}
