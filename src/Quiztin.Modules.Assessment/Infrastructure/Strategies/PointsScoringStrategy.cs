using System.Collections.Generic;
using System.Linq;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Strategies;

namespace Quiztin.Modules.Assessment.Infrastructure.Strategies
{
    /// <summary>
    /// All-or-nothing per question: full points for a correct answer, zero otherwise.
    /// Grading itself lives on the Question (Information Expert); this strategy just
    /// composes the per-question verdicts into a total.
    /// </summary>
    public class PointsScoringStrategy : IScoringStrategy
    {
        public void Score(QuizAttempt attempt, IReadOnlyList<Question> questions)
        {
            var byId = questions.ToDictionary(q => q.Id);
            decimal total = 0;

            foreach (var answer in attempt.Answers)
            {
                if (byId.TryGetValue(answer.QuestionId, out var question) && question.IsCorrect(answer.ProvidedAnswer))
                {
                    answer.IsCorrect = true;
                    answer.PointsAwarded = question.Points;
                    total += question.Points;
                }
                else
                {
                    answer.IsCorrect = false;
                    answer.PointsAwarded = 0;
                }
            }

            attempt.TotalScore = total;
        }
    }
}
