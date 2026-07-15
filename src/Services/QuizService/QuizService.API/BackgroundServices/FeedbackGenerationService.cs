using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuizService.Application.Interfaces;
using QuizService.Domain.Enums;
using QuizService.Domain.Interfaces;
using QuizService.Domain.Strategies;

namespace QuizService.API.BackgroundServices
{
    /// <summary>
    /// Generates feedback off the submit path (spec 0005). It reads graded attempt ids
    /// from the feedback queue, and for each one opens its own DI scope (a fresh scoped
    /// DbContext and repositories), skips the attempt if feedback is already Ready
    /// (idempotent under a redelivered graded event, AC-7), runs the feedback strategy
    /// (AI with a deterministic fallback), then marks the attempt Reviewable and Ready.
    /// Each attempt is wrapped in its own try and catch, so a bad model response or a
    /// concurrency conflict is logged (without content, security.md §6) and skipped,
    /// never crashing the worker (AC-2, AC-7).
    /// </summary>
    public sealed class FeedbackGenerationService : BackgroundService
    {
        private readonly IFeedbackQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FeedbackGenerationService> _logger;

        public FeedbackGenerationService(
            IFeedbackQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<FeedbackGenerationService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Guid attemptId;
                try
                {
                    attemptId = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break; // shutting down
                }

                try
                {
                    await GenerateForAttemptAsync(attemptId, stoppingToken);
                }
                catch (Exception ex)
                {
                    // No answers, feedback, or prompts in the log (security.md §6). One bad
                    // attempt is skipped; the worker keeps running (AC-7).
                    _logger.LogError(ex, "Feedback generation failed for attempt {AttemptId}; skipping.", attemptId);
                }
            }
        }

        private async Task GenerateForAttemptAsync(Guid attemptId, CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var provider = scope.ServiceProvider;
            var attemptRepository = provider.GetRequiredService<IQuizAttemptRepository>();
            var quizRepository = provider.GetRequiredService<IQuizRepository>();
            var feedbackStrategy = provider.GetRequiredService<IFeedbackStrategy>();

            var attempt = await attemptRepository.GetByIdAsync(attemptId);
            if (attempt is null)
            {
                _logger.LogWarning("Feedback: attempt {AttemptId} not found; skipping.", attemptId);
                return;
            }

            attempt.LoadState();
            if (attempt.FeedbackStatus == FeedbackStatus.Ready)
            {
                return; // a redelivered graded event: already done, safe no op (AC-7)
            }

            var quiz = await quizRepository.GetByIdAsync(attempt.QuizId);
            if (quiz is null)
            {
                _logger.LogWarning("Feedback: quiz {QuizId} not found for attempt {AttemptId}; skipping.", attempt.QuizId, attemptId);
                return;
            }

            var questions = quiz.Questions.ToList();

            await feedbackStrategy.GenerateAsync(attempt, questions, stoppingToken);
            attempt.MarkFeedbackReady();
            await attemptRepository.UpdateAsync(attempt);
        }
    }
}
