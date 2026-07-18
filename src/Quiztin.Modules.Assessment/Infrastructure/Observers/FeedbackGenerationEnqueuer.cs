using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Application.Interfaces;
using Quiztin.Modules.Assessment.Domain.Events;

namespace Quiztin.Modules.Assessment.Infrastructure.Observers
{
    /// <summary>
    /// On the post commit graded event, enqueue the attempt for background feedback
    /// generation. This is what keeps submit fast (AC-1): the heavy Claude call happens
    /// off the request thread, in the hosted worker.
    /// </summary>
    public class FeedbackGenerationEnqueuer : Quiztin.Modules.Assessment.Domain.Observers.IObserver<QuizAttemptGradedEvent>
    {
        private readonly IFeedbackQueue _queue;

        public FeedbackGenerationEnqueuer(IFeedbackQueue queue)
        {
            _queue = queue;
        }

        public Task UpdateAsync(QuizAttemptGradedEvent domainEvent)
        {
            _queue.Enqueue(domainEvent.AttemptId);
            return Task.CompletedTask;
        }
    }
}
