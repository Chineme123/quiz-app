using Quiztin.Modules.Assessment.Application.DTOs;

namespace Quiztin.Modules.Assessment.Application.Results
{
    /// <summary>
    /// Outcome of a generate request (spec 0009). The generate call itself always returns a batch
    /// on success (even the deterministic empty templates), so its failure modes are the same
    /// owner and lock gates the rest of authoring uses, plus Invalid for a blank topic.
    ///
    /// A quiz that does not exist and one owned by another teacher both report NotFound, so a
    /// quiz's existence never leaks (AC-10). Locked (409) is returned once the quiz has an attempt:
    /// there is no point generating for a quiz whose questions can no longer change (AC-9).
    /// </summary>
    public enum GenerationOutcome
    {
        Ok,
        NotFound,
        Invalid,
        Locked
    }

    public class GenerationResult
    {
        public GenerationOutcome Outcome { get; set; }
        public GeneratedDraftDto? Draft { get; set; }
        public string? Error { get; set; }

        public static GenerationResult Ok(GeneratedDraftDto draft) => new() { Outcome = GenerationOutcome.Ok, Draft = draft };
        public static GenerationResult NotFound() => new() { Outcome = GenerationOutcome.NotFound };
        public static GenerationResult Invalid(string error) => new() { Outcome = GenerationOutcome.Invalid, Error = error };
        public static GenerationResult Locked() => new() { Outcome = GenerationOutcome.Locked };
    }
}
