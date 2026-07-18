namespace Quiztin.Modules.Assessment.Domain.Enums
{
    /// <summary>
    /// Who wrote a given answer's feedback: the model (<see cref="Ai"/>) or the
    /// deterministic fallback (<see cref="Deterministic"/>). Correct answers and any
    /// answer written during a Claude outage are <see cref="Deterministic"/> (spec 0005,
    /// AC-2, AC-3, AC-4). Both are shown to the student the same way.
    /// </summary>
    public enum FeedbackSource
    {
        Ai,
        Deterministic
    }
}
