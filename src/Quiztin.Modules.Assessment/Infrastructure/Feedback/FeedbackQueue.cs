using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Application.Interfaces;

namespace Quiztin.Modules.Assessment.Infrastructure.Feedback
{
    /// <summary>
    /// An in memory, unbounded channel of attempt ids awaiting feedback. Registered as a
    /// singleton so the graded event observer (the writer) and the background worker (the
    /// single reader) share one channel. Not durable by design in v1 (no outbox); a
    /// restart loses anything still queued (spec 0005 Consequences and Follow-up).
    /// </summary>
    public class FeedbackQueue : IFeedbackQueue
    {
        private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
            new UnboundedChannelOptions { SingleReader = true });

        public void Enqueue(Guid attemptId) => _channel.Writer.TryWrite(attemptId);

        public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
            => _channel.Reader.ReadAsync(cancellationToken);
    }
}
