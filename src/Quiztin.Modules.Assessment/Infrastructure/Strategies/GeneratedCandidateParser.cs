using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Factories;

namespace Quiztin.Modules.Assessment.Infrastructure.Strategies
{
    /// <summary>
    /// Turns untrusted model output into valid question candidates (spec 0009, AC-6). The JSON
    /// array is parsed element by element, tolerantly: one bad element is one dropped candidate,
    /// never a failed batch (unlike the feedback path's one shot Deserialize&lt;List&lt;T&gt;&gt;,
    /// which fails whole on a single bad element). Each survivor is validated through the same
    /// QuestionFactory.TryCreate rules a manual add uses, and its text is length capped and kept
    /// as plain text. Public and static so this rule set can be tested without a live model call.
    /// </summary>
    public static class GeneratedCandidateParser
    {
        public const int MaxPromptChars = 1000;
        public const int MaxOptionChars = 500;

        public static List<GeneratedCandidate> Parse(string raw)
        {
            var candidates = new List<GeneratedCandidate>();
            var json = ExtractJsonArray(raw);
            if (json is null) return candidates;

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch (JsonException) { return candidates; }

            using (doc)
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return candidates;
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var candidate = TryParseCandidate(element);
                    if (candidate is not null) candidates.Add(candidate);
                }
            }
            return candidates;
        }

        private static GeneratedCandidate? TryParseCandidate(JsonElement element)
        {
            try
            {
                if (element.ValueKind != JsonValueKind.Object) return null;

                var type = GetString(element, "questionType");
                var prompt = Cap(GetString(element, "prompt"), MaxPromptChars);
                var points = GetInt(element, "points") ?? 0;

                switch (type)
                {
                    case "MultipleChoice":
                        var options = GetStringList(element, "options")?
                            .Select(o => Cap(o, MaxOptionChars) ?? string.Empty).ToList();
                        var index = GetInt(element, "correctOptionIndex") ?? -1;
                        if (!QuestionFactory.TryCreateMultipleChoice(prompt, points, options, index).IsSuccess)
                            return null;
                        return new GeneratedCandidate
                        {
                            QuestionType = type,
                            Prompt = prompt ?? string.Empty,
                            Points = points,
                            Options = options,
                            CorrectOptionIndex = index
                        };
                    case "TrueFalse":
                        var boolAnswer = GetBool(element, "correctAnswerBool") ?? false;
                        if (!QuestionFactory.TryCreateTrueFalse(prompt, points, boolAnswer).IsSuccess)
                            return null;
                        return new GeneratedCandidate
                        {
                            QuestionType = type,
                            Prompt = prompt ?? string.Empty,
                            Points = points,
                            CorrectAnswerBool = boolAnswer
                        };
                    case "ShortAnswer":
                        var text = Cap(GetString(element, "correctAnswerText"), MaxOptionChars);
                        if (!QuestionFactory.TryCreateShortAnswer(prompt, points, text).IsSuccess)
                            return null;
                        return new GeneratedCandidate
                        {
                            QuestionType = type,
                            Prompt = prompt ?? string.Empty,
                            Points = points,
                            CorrectAnswerText = text
                        };
                    default:
                        return null;
                }
            }
            catch (Exception)
            {
                // A single malformed element is dropped, never fatal to the batch.
                return null;
            }
        }

        private static string? GetString(JsonElement e, string name)
            => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

        private static int? GetInt(JsonElement e, string name)
            => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var v)
                ? v : (int?)null;

        private static bool? GetBool(JsonElement e, string name)
            => e.TryGetProperty(name, out var p) && (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False)
                ? p.GetBoolean() : (bool?)null;

        private static List<string>? GetStringList(JsonElement e, string name)
        {
            if (!e.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.Array) return null;
            var list = new List<string>();
            foreach (var item in p.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String) list.Add(item.GetString() ?? string.Empty);
            return list;
        }

        private static string? Cap(string? text, int max)
        {
            if (text is null) return null;
            var trimmed = text.Trim();
            return trimmed.Length <= max ? trimmed : trimmed.Substring(0, max);
        }

        // The model may wrap the array in prose or a markdown fence; take the outermost [ ].
        private static string? ExtractJsonArray(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var start = raw.IndexOf('[');
            var end = raw.LastIndexOf(']');
            return start >= 0 && end > start ? raw.Substring(start, end - start + 1) : null;
        }
    }
}
