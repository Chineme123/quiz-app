using System.Collections.Generic;
using QuizService.Domain.Entities;

namespace QuizService.Domain.Strategies
{
    public interface IFeedbackStrategy
    {
        void Generate(QuizAttempt attempt);
    }
}
