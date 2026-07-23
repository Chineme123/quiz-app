using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Interfaces;
using Quiztin.Modules.Assessment.Infrastructure.Configuration;

namespace Quiztin.Modules.Assessment.Infrastructure.Strategies
{
    /// <summary>
    /// The deterministic generation fallback (spec 0009, AC-4): with AI off, no key, or a failed
    /// model call, the teacher gets the requested number of empty, editable question templates
    /// rather than nothing. The templates are intentionally blank, so they are NOT validated (an
    /// empty prompt would fail); the teacher fills them in before accepting.
    /// </summary>
    public class TemplateQuestionGenerationStrategy : IQuestionGenerationStrategy
    {
        private readonly GenerationOptions _options;

        public TemplateQuestionGenerationStrategy(IOptions<GenerationOptions> options)
        {
            _options = options.Value;
        }

        public Task<IReadOnlyList<GeneratedCandidate>> GenerateAsync(string topic, string difficulty, int count)
        {
            var capped = Math.Clamp(count, 1, Math.Max(1, _options.MaxCount));
            var candidates = new List<GeneratedCandidate>(capped);
            for (var i = 0; i < capped; i++)
            {
                candidates.Add(new GeneratedCandidate
                {
                    QuestionType = "MultipleChoice",
                    Prompt = string.Empty,
                    Points = 1,
                    Options = new List<string> { string.Empty, string.Empty },
                    CorrectOptionIndex = 0
                });
            }
            return Task.FromResult<IReadOnlyList<GeneratedCandidate>>(candidates);
        }
    }
}
