namespace Quiztin.Modules.Assessment.Infrastructure.Configuration
{
    /// <summary>
    /// AI question generation settings (spec 0009), bound from the "Generation" config section.
    /// Parallel to <see cref="FeedbackOptions"/> plus <see cref="AnthropicOptions"/>: when
    /// <see cref="AiEnabled"/> is false, or no Anthropic key is present, the deterministic
    /// template strategy produces empty editable questions instead, so the authoring flow builds
    /// and runs before the key is provisioned (AC-4). The API key itself is shared and lives on
    /// <see cref="AnthropicOptions.ApiKey"/>; only the higher stakes generation model is separate.
    /// </summary>
    public class GenerationOptions
    {
        public const string SectionName = "Generation";

        public bool AiEnabled { get; set; }

        /// <summary>The generation model id. Separate from the feedback model so the higher
        /// stakes, low volume generation call can use a stronger tier (spec 0009).</summary>
        public string Model { get; set; } = "claude-opus-4-8";

        /// <summary>The most candidates one request may ask for; a request above this is capped
        /// so it cannot blow up the token spend.</summary>
        public int MaxCount { get; set; } = 20;

        /// <summary>The cap on attached source text length (spec 0009, AC-7): extraction stops
        /// here, and pasted plus extracted text is trimmed to it before it reaches the model.</summary>
        public int MaxSourceChars { get; set; } = 10000;

        /// <summary>The largest source file accepted, enforced at the request pipeline level so
        /// the framework never buffers a huge body before the check runs (AC-7). Default 5 MB.</summary>
        public long MaxUploadBytes { get; set; } = 5 * 1024 * 1024;

        /// <summary>The wall-clock limit on parsing one file, so a decompression bomb cannot hang
        /// the extractor (AC-7).</summary>
        public int ExtractionTimeoutSeconds { get; set; } = 10;

        /// <summary>Per call timeout before the deterministic template fallback takes over.</summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}
