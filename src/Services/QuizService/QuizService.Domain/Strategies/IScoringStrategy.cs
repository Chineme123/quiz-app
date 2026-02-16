using QuizService.Domain.Entities;

namespace QuizService.Domain.Strategies
{
    public interface IScoringStrategy
    {
        void Score(QuizAttempt attempt);
    }
}
