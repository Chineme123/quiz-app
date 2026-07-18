using System.Collections.Generic;
using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.Strategies
{
    public interface IScoringStrategy
    {
        // `questions` carry the correct answers; `attempt.Answers` carry the student's.
        // The strategy grades each answer and sets the attempt's TotalScore.
        void Score(QuizAttempt attempt, IReadOnlyList<Question> questions);
    }
}
