using System;
using System.Threading;
using System.Threading.Tasks;

namespace Quiztin.Modules.Assessment.Application.Interfaces
{
    /// <summary>
    /// Hands graded attempt ids from the post commit graded event to the background
    /// feedback worker, so submitting never waits on the model (spec 0005, AC-1). In
    /// memory only in v1: an id enqueued but not processed before a restart is lost and
    /// the attempt stays Pending (no outbox yet; see the spec Follow-up).
    /// </summary>
    public interface IFeedbackQueue
    {
        void Enqueue(Guid attemptId);
        ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
    }
}
