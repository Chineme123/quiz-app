namespace QuizService.Infrastructure.Configuration
{
    /// <summary>
    /// Feedback feature flags (spec 0005). When <see cref="AiEnabled"/> is false, or no
    /// Anthropic key is present, the deterministic strategy handles every attempt, so the
    /// loop builds and runs before the key is provisioned (AC-4).
    /// </summary>
    public class FeedbackOptions
    {
        public const string SectionName = "Feedback";

        public bool AiEnabled { get; set; }
    }
}
