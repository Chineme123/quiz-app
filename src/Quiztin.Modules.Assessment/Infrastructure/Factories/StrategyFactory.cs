using System;
using Quiztin.Modules.Assessment.Domain.Factories;
using Quiztin.Modules.Assessment.Domain.Strategies;
using Quiztin.Modules.Assessment.Infrastructure.Strategies;

namespace Quiztin.Modules.Assessment.Infrastructure.Factories
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

        // Feedback strategies are resolved from DI now, not built here: the AI strategy
        // needs an injected Anthropic client and the deterministic fallback, which a
        // parameterless `new` cannot supply (spec 0005).
    }
}
