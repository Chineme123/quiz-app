using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Enums;
using Quiztin.Modules.Assessment.Domain.Strategies;

namespace Quiztin.Modules.Assessment.Infrastructure.Strategies
{
    /// <summary>
    /// Deterministic, encouraging fallback feedback. Brand voice: frame a miss as
    /// something "to review", never punitive. This keeps the loop working when the
    /// real AI feedback strategy is unavailable (foundation.md §7 #6 fallback), and the
    /// AI strategy also delegates here for correct answers and for a whole attempt when
    /// Claude fails (spec 0005, AC-3, AC-4). Every answer it writes is sourced Deterministic.
    /// </summary>
    public class StandardFeedbackStrategy : IFeedbackStrategy
    {
        public Task GenerateAsync(QuizAttempt attempt, IReadOnlyList<Question> questions, CancellationToken cancellationToken = default)
        {
            foreach (var answer in attempt.Answers)
            {
                answer.Feedback = answer.IsCorrect
                    ? "Nice — that's right."
                    : "Not quite — worth another look at this one.";
                answer.FeedbackSource = FeedbackSource.Deterministic;
            }

            return Task.CompletedTask;
        }
    }
}
