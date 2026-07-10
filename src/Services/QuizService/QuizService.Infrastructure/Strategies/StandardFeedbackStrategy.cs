using System.Collections.Generic;
using QuizService.Domain.Entities;
using QuizService.Domain.Strategies;

namespace QuizService.Infrastructure.Strategies
{
    /// <summary>
    /// Deterministic, encouraging fallback feedback. Brand voice: frame a miss as
    /// something "to review", never punitive. This keeps the loop working when the
    /// real AI feedback strategy is unavailable (foundation.md §7 #6 fallback).
    /// </summary>
    public class StandardFeedbackStrategy : IFeedbackStrategy
    {
        public void Generate(QuizAttempt attempt, IReadOnlyList<Question> questions)
        {
            foreach (var answer in attempt.Answers)
            {
                answer.Feedback = answer.IsCorrect
                    ? "Nice — that's right."
                    : "Not quite — worth another look at this one.";
            }
        }
    }
}
