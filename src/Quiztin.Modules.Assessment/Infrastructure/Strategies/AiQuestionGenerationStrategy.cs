using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Interfaces;
using Quiztin.Modules.Assessment.Infrastructure.Configuration;

namespace Quiztin.Modules.Assessment.Infrastructure.Strategies
{
    /// <summary>
    /// Real question generation via Claude (spec 0009). Mirrors the feedback strategy's wiring: a
    /// pooled HttpClient from IHttpClientFactory, a per call timeout that degrades to the
    /// deterministic template fallback (AC-4), and only task necessary content across the boundary
    /// (topic, difficulty, count; no identity, security.md section 2). The model's output is
    /// untrusted and handed to <see cref="GeneratedCandidateParser"/>, which parses element by
    /// element and validates each candidate through the same QuestionFactory rules a manual add
    /// uses, so a partly bad batch still yields its good candidates (AC-6). Prompts, responses,
    /// and the key are never logged (security.md section 6).
    /// </summary>
    public sealed class AiQuestionGenerationStrategy : IQuestionGenerationStrategy
    {
        private readonly TemplateQuestionGenerationStrategy _fallback;
        private readonly ILogger<AiQuestionGenerationStrategy> _logger;
        private readonly GenerationOptions _options;
        private readonly AnthropicClient _client;

        public AiQuestionGenerationStrategy(
            HttpClient httpClient,
            IOptions<GenerationOptions> options,
            IOptions<AnthropicOptions> anthropicOptions,
            TemplateQuestionGenerationStrategy fallback,
            ILogger<AiQuestionGenerationStrategy> logger)
        {
            _options = options.Value;
            _fallback = fallback;
            _logger = logger;
            // Pooled, long-lived client from IHttpClientFactory; the per-call timeout that
            // degrades to the template fallback is set on it at registration (the SDK call takes
            // no CancellationToken). The key is shared with the feedback path.
            _client = new AnthropicClient(new APIAuthentication(anthropicOptions.Value.ApiKey), httpClient);
        }

        public async Task<IReadOnlyList<GeneratedCandidate>> GenerateAsync(string topic, string difficulty, int count, string? sourceText = null)
        {
            var capped = Math.Clamp(count, 1, Math.Max(1, _options.MaxCount));

            var raw = await TryCallAsync(topic, difficulty, capped, sourceText);
            if (raw is null)
            {
                // Unavailable, timeout, or error after one retry: empty editable templates (AC-4).
                return await _fallback.GenerateAsync(topic, difficulty, capped);
            }

            var candidates = GeneratedCandidateParser.Parse(raw);
            // The model returned but produced nothing usable: still give the teacher something.
            if (candidates.Count == 0)
                return await _fallback.GenerateAsync(topic, difficulty, capped);

            return candidates;
        }

        private async Task<string?> TryCallAsync(string topic, string difficulty, int count, string? sourceText)
        {
            // One retry: a transient blip should not drop the whole request to templates.
            for (var attemptNo = 0; attemptNo < 2; attemptNo++)
            {
                try
                {
                    return await CallClaudeAsync(topic, difficulty, count, sourceText);
                }
                catch (Exception ex) when (attemptNo == 0)
                {
                    _logger.LogWarning(ex, "AI question generation call failed (try 1 of 2); retrying once.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI question generation call failed after retry; using empty templates.");
                }
            }
            return null;
        }

        private async Task<string> CallClaudeAsync(string topic, string difficulty, int count, string? sourceText)
        {
            // Only task necessary content, no identity (security.md section 2).
            var userMessage = $"Topic: {topic}\nDifficulty: {difficulty}\nGenerate exactly {count} questions.";
            if (!string.IsNullOrWhiteSpace(sourceText))
            {
                var capped = sourceText.Length > _options.MaxSourceChars
                    ? sourceText.Substring(0, _options.MaxSourceChars)
                    : sourceText;
                userMessage += "\n\nBase the questions on this source material:\n" + capped;
            }
            var parameters = new MessageParameters
            {
                Model = _options.Model,
                MaxTokens = Math.Clamp(count * 300, 1024, 8192),
                Stream = false,
                System = new List<SystemMessage> { new SystemMessage(SystemPrompt) },
                Messages = new List<Message> { new Message(RoleType.User, userMessage) }
            };
            var result = await _client.Messages.GetClaudeMessageAsync(parameters);
            return result.Content?.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;
        }

        private const string SystemPrompt =
            "You write quiz questions for a teacher. You are given a topic, a difficulty, and a count. " +
            "Return ONLY a JSON array, no prose and no markdown, with exactly the requested number of elements. " +
            "Each element is one of: " +
            "{\"questionType\":\"MultipleChoice\",\"prompt\":\"...\",\"points\":<int>,\"options\":[\"..\",\"..\"],\"correctOptionIndex\":<int>} " +
            "(at least two options, correctOptionIndex within range), " +
            "{\"questionType\":\"TrueFalse\",\"prompt\":\"...\",\"points\":<int>,\"correctAnswerBool\":<true|false>}, or " +
            "{\"questionType\":\"ShortAnswer\",\"prompt\":\"...\",\"points\":<int>,\"correctAnswerText\":\"..\"}. " +
            "Points must be a positive integer. Use plain text only, never HTML. Vary the question types.";
    }
}
