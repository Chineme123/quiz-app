namespace Quiztin.Modules.Assessment.Infrastructure.Configuration
{
    /// <summary>
    /// Anthropic API settings for the AI feedback strategy (spec 0005). Bound from the
    /// "Anthropic" config section. The API key comes from user secrets or environment,
    /// never committed and never sent to the frontend (security.md §3). QuizService is
    /// the only service that holds it.
    /// </summary>
    public class AnthropicOptions
    {
        public const string SectionName = "Anthropic";

        public string ApiKey { get; set; } = string.Empty;

        /// <summary>The model id. A fast, cost effective tier for high volume feedback.</summary>
        public string FeedbackModel { get; set; } = "claude-haiku-4-5";

        /// <summary>Per call timeout before the deterministic fallback takes over.</summary>
        public int TimeoutSeconds { get; set; } = 10;
    }
}
