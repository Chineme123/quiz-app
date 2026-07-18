using Quiztin.Modules.Assessment.Domain.Strategies;

namespace Quiztin.Modules.Assessment.Domain.Factories
{
    public interface IStrategyFactory
    {
        IScoringStrategy GetScoringStrategy(string strategyName);
    }
}
