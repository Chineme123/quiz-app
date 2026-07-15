using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuizService.Domain.Entities;
using QuizService.Domain.Enums;
using QuizService.Domain.Strategies;
using QuizService.Infrastructure.Configuration;

namespace QuizService.Infrastructure.Strategies
{
    /// <summary>
    /// AI feedback via Claude (spec 0005). One call per attempt for the incorrect and
    /// partial answers, batched with a local index so the response maps back by position,
    /// never by a stored id. Correct answers get the deterministic confirmation with no
    /// model call (AC-3). On any failure (unavailable, timeout, error after one retry, or
    /// a malformed or mismatched response) the whole attempt keeps the deterministic
    /// feedback (AC-4). Only { index, question, correct answer, student answer } crosses
    /// the boundary: no identity, no ids (AC-5). Feedback is length capped and stored as
    /// plain text (AC-10).
    /// </summary>
    public sealed class AiFeedbackStrategy : IFeedbackStrategy, IDisposable
    {
        private const int MaxFeedbackChars = 600;
        private const int MaxTokens = 1024;

        private readonly StandardFeedbackStrategy _fallback;
        private readonly ILogger<AiFeedbackStrategy> _logger;
        private readonly AnthropicOptions _options;
        private readonly HttpClient _httpClient;
        private readonly AnthropicClient _client;

        public AiFeedbackStrategy(
            IOptions<AnthropicOptions> options,
            StandardFeedbackStrategy fallback,
            ILogger<AiFeedbackStrategy> logger)
        {
            _options = options.Value;
            _fallback = fallback;
            _logger = logger;
            // The per call timeout is how "Claude times out" degrades to the fallback (AC-4).
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)) };
            _client = new AnthropicClient(new APIAuthentication(_options.ApiKey), _httpClient);
        }

        public async Task GenerateAsync(QuizAttempt attempt, IReadOnlyList<Question> questions, CancellationToken cancellationToken = default)
        {
            // Lay a deterministic baseline over EVERY answer first: the correct confirmations
            // and the encouraging "worth another look". This is also the whole attempt
            // fallback, so if the model fails below, every answer already has feedback and a
            // source (AC-3, AC-4). On success we upgrade only the wrong answers to AI text.
            await _fallback.GenerateAsync(attempt, questions, cancellationToken);

            var incorrect = attempt.Answers.Where(a => !a.IsCorrect).ToList();
            if (incorrect.Count == 0) return; // all correct → no model call (AC-3)

            var byId = questions.ToDictionary(q => q.Id);
            var items = new List<FeedbackRequestItem>(incorrect.Count);
            for (var i = 0; i < incorrect.Count; i++)
            {
                if (!byId.TryGetValue(incorrect[i].QuestionId, out var question))
                {
                    // Cannot build a safe payload for this attempt; keep the baseline.
                    return;
                }
                items.Add(new FeedbackRequestItem(
                    i + 1,
                    question.Prompt,
                    question.GetCorrectAnswerText(),
                    incorrect[i].ProvidedAnswer));
            }

            var mapped = await TryGenerateAsync(items, cancellationToken);

            // Reject a missing, extra, or misaligned response and keep the baseline (AC-4).
            if (mapped is null || mapped.Count != items.Count || items.Any(it => !mapped.ContainsKey(it.Index)))
            {
                if (mapped is not null)
                    _logger.LogWarning("AI feedback for attempt {AttemptId} did not line up; using deterministic feedback.", attempt.Id);
                return;
            }

            for (var i = 0; i < incorrect.Count; i++)
            {
                incorrect[i].Feedback = Cap(mapped[i + 1]);
                incorrect[i].FeedbackSource = FeedbackSource.Ai;
            }
        }

        private async Task<Dictionary<int, string>?> TryGenerateAsync(List<FeedbackRequestItem> items, CancellationToken cancellationToken)
        {
            // One retry: a transient blip should not drop the whole attempt to deterministic.
            for (var attemptNo = 0; attemptNo < 2; attemptNo++)
            {
                try
                {
                    var raw = await CallClaudeAsync(items, cancellationToken);
                    return ParseAndMap(raw); // null (malformed) is not retried; the caller falls back
                }
                catch (Exception ex) when (attemptNo == 0)
                {
                    _logger.LogWarning(ex, "AI feedback call failed (try 1 of 2); retrying once.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI feedback call failed after retry; using deterministic feedback.");
                }
            }
            return null;
        }

        private async Task<string> CallClaudeAsync(List<FeedbackRequestItem> items, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Serialize(items);
            var parameters = new MessageParameters
            {
                Model = _options.FeedbackModel,
                MaxTokens = MaxTokens,
                Stream = false,
                System = new List<SystemMessage> { new SystemMessage(SystemPrompt) },
                Messages = new List<Message> { new Message(RoleType.User, payload) }
            };
            // The HttpClient timeout bounds the call; cancellation on shutdown is handled by
            // the worker loop. (The SDK method takes no CancellationToken in this version.)
            var result = await _client.Messages.GetClaudeMessageAsync(parameters);
            return result.Content?.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;
        }

        private static Dictionary<int, string>? ParseAndMap(string raw)
        {
            var json = ExtractJsonArray(raw);
            if (json is null) return null;
            try
            {
                var items = JsonSerializer.Deserialize<List<FeedbackResponseItem>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (items is null) return null;

                var map = new Dictionary<int, string>();
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.Feedback)) return null;
                    map[item.Index] = item.Feedback.Trim();
                }
                return map;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // The model may wrap the array in prose or a markdown fence; take the outermost [ ].
        private static string? ExtractJsonArray(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var start = raw.IndexOf('[');
            var end = raw.LastIndexOf(']');
            return start >= 0 && end > start ? raw.Substring(start, end - start + 1) : null;
        }

        private static string Cap(string text)
        {
            var trimmed = text.Trim();
            return trimmed.Length <= MaxFeedbackChars
                ? trimmed
                : trimmed.Substring(0, MaxFeedbackChars).TrimEnd() + "…";
        }

        private const string SystemPrompt =
            "You are a warm, encouraging tutor writing short feedback for a student who got a quiz question wrong. " +
            "For each item you are given a local index, the question, the correct answer, and the student's answer. " +
            "Write one or two supportive sentences that explain the idea and gently point toward the correct answer. " +
            "Frame it as something to review, never as a failure, and never mention the student by any name. " +
            "Return ONLY a JSON array, no prose and no markdown, where each element is " +
            "{\"index\": <the item's index>, \"feedback\": \"<your feedback as plain text>\"}. " +
            "Include exactly one element for every item, echoing its index. Do not use HTML.";

        private sealed record FeedbackRequestItem(int Index, string Question, string CorrectAnswer, string StudentAnswer);

        private sealed record FeedbackResponseItem
        {
            public int Index { get; init; }
            public string Feedback { get; init; } = string.Empty;
        }

        public void Dispose() => _httpClient.Dispose();
    }
}
