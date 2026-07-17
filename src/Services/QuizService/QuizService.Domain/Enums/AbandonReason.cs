namespace QuizService.Domain.Enums
{
    /// <summary>
    /// Why an attempt was abandoned (foundation §69 triggers, spec 0006 AC-15).
    /// Stored so the attempt limit can tell the reasons apart: only <see cref="Superseded"/>
    /// consumes an attempt against <c>Quiz.MaxAttempts</c>, because restarting is the one
    /// abandon a student controls, so it is the one that should cost them. Without it a
    /// student could start, read the questions, restart, and repeat for free.
    ///
    /// Trigger 1 (expiry) is absent by design: it no longer abandons, it grades the saved
    /// answers (spec 0006 AC-12). Trigger 2 (window close) is absent because it gates
    /// starting only and never abandons a running attempt (AC-14).
    /// </summary>
    public enum AbandonReason
    {
        /// <summary>Trigger 4: a new attempt was started over this unfinished one.</summary>
        Superseded,

        /// <summary>Trigger 3: the student explicitly quit. No UI offers this in v1.</summary>
        Quit
    }
}
