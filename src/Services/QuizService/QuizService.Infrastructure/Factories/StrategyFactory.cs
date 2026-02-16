using System;
using QuizService.Domain.Factories;
using QuizService.Domain.Strategies;
using QuizService.Infrastructure.Strategies;

namespace QuizService.Infrastructure.Factories
{
    public class StrategyFactory : IStrategyFactory
    {
        // Simple implementation. In real app, could cache instances or use DI container
        
        public IScoringStrategy GetScoringStrategy(string strategyName)
        {
            return strategyName switch
            {
                "Points" => new PointsScoringStrategy(),
                // Add others here
                _ => new PointsScoringStrategy() // Default
            };
        }

        public IFeedbackStrategy GetFeedbackStrategy(string strategyName)
        {
             return strategyName switch
            {
                "Standard" => new StandardFeedbackStrategy(),
                // Add others here
                _ => new StandardFeedbackStrategy() // Default
            };
        }
    }
}
