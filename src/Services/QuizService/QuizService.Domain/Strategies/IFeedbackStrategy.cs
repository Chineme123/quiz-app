using System.Collections.Generic;
using QuizService.Domain.Entities;

namespace QuizService.Domain.Strategies
{
    public interface IFeedbackStrategy
    {
        // `questions` are passed so a strategy (esp. the future AI one) can reference the
        // question + correct answer when writing per-answer feedback.
        void Generate(QuizAttempt attempt, IReadOnlyList<Question> questions);
    }
}
