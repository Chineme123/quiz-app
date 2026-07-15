using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuizService.Domain.Entities;

namespace QuizService.Domain.Strategies
{
    public interface IFeedbackStrategy
    {
        // Writes each answer's Feedback and FeedbackSource on the attempt. Async because
        // the AI strategy calls the model over the network; the deterministic strategy
        // just completes. `questions` carry the question text and correct answer the
        // feedback references. Runs in the background, off the submit path (spec 0005).
        Task GenerateAsync(QuizAttempt attempt, IReadOnlyList<Question> questions, CancellationToken cancellationToken = default);
    }
}
