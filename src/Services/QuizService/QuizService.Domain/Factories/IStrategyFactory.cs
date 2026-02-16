using QuizService.Domain.Strategies;

namespace QuizService.Domain.Factories
{
    public interface IStrategyFactory
    {
        IScoringStrategy GetScoringStrategy(string strategyName);
        IFeedbackStrategy GetFeedbackStrategy(string strategyName);
    }
}
