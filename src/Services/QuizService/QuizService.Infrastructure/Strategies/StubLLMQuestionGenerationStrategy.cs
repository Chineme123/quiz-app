using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuizService.Domain.Entities;
using QuizService.Domain.Factories;
using QuizService.Domain.Interfaces;

namespace QuizService.Infrastructure.Strategies
{
    public class StubLLMQuestionGenerationStrategy : IQuestionGenerationStrategy
    {
        public string ModeName => "LLM";

        public Task<List<Question>> GenerateQuestionsAsync(string topic, int count, string difficulty)
        {
            var questions = new List<Question>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                // Simple stub logic to alternate types or just return MCQs
                int type = i % 3;
                if (type == 0)
                {
                    questions.Add(QuestionFactory.CreateMultipleChoice(
                        $"[AI-{difficulty}] What is a key concept in {topic} (Question {i + 1})?",
                        10,
                        new List<string> { "Concept A", "Concept B", "Concept C", "Concept D" },
                        0
                    ));
                }
                else if (type == 1)
                {
                    questions.Add(QuestionFactory.CreateTrueFalse(
                        $"[AI-{difficulty}] Is {topic} hard (Question {i + 1})?",
                        5,
                        true
                    ));
                }
                else
                {
                    questions.Add(QuestionFactory.CreateShortAnswer(
                        $"[AI-{difficulty}] Explain {topic} briefly (Question {i + 1}).",
                        15,
                        "It is interesting."
                    ));
                }
            }

            return Task.FromResult(questions);
        }
    }
}
