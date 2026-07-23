using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Factories;
using Quiztin.Modules.Assessment.Domain.Interfaces;

namespace Quiztin.Modules.Assessment.Infrastructure.Strategies
{
    public class StubLLMQuestionGenerationStrategy : IQuestionGenerationStrategy
    {
        public string ModeName => "LLM";

        public Task<List<Question>> GenerateQuestionsAsync(string topic, int count, string difficulty)
        {
            var questions = new List<Question>();

            for (int i = 0; i < count; i++)
            {
                // Simple stub logic: alternate the three question types.
                int type = i % 3;
                var result = type switch
                {
                    0 => QuestionFactory.TryCreateMultipleChoice(
                            $"[AI-{difficulty}] What is a key concept in {topic} (Question {i + 1})?",
                            10,
                            new List<string> { "Concept A", "Concept B", "Concept C", "Concept D" },
                            0),
                    1 => QuestionFactory.TryCreateTrueFalse(
                            $"[AI-{difficulty}] Is {topic} hard (Question {i + 1})?",
                            5,
                            true),
                    _ => QuestionFactory.TryCreateShortAnswer(
                            $"[AI-{difficulty}] Explain {topic} briefly (Question {i + 1}).",
                            15,
                            "It is interesting."),
                };

                // Every stub candidate is well formed, so this always succeeds; routing it through
                // the same TryCreate the manual path uses keeps generation on one validation rule
                // set (spec 0009), and the real generator (task 3) drops a bad candidate the same way.
                if (result.Question is { } question)
                    questions.Add(question);
            }

            return Task.FromResult(questions);
        }
    }
}
