using QuizService.Domain.Entities;
using QuizService.Domain.Strategies;

namespace QuizService.Infrastructure.Strategies
{
    public class StandardFeedbackStrategy : IFeedbackStrategy
    {
        public void Generate(QuizAttempt attempt)
        {
            foreach (var answer in attempt.Answers)
            {
                if (answer.IsCorrect)
                {
                    answer.Feedback = "Correct! Great job.";
                }
                else
                {
                    answer.Feedback = "Incorrect. Please review the material.";
                }
            }
        }
    }
}
