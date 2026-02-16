using System;
using System.Threading.Tasks;
using QuizService.Domain.Entities;
using QuizService.Domain.Interfaces;
using QuizService.Domain.Factories;
using QuizService.Application.Commands;
using QuizService.Application.DTOs;
using QuizService.Application.Invokers;
using QuizService.Domain.Events;
using QuizService.Domain.Observers; // Assuming existing or we need to add dispatch logic

namespace QuizService.Application.Facades
{
    public class TakeQuizFacade
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IQuizAttemptRepository _attemptRepository;
        private readonly IStrategyFactory _strategyFactory;
        private readonly QuizCommandInvoker _commandInvoker;
        private readonly IEventDispatcher _eventDispatcher;

        public TakeQuizFacade(
            IQuizRepository quizRepository, 
            IQuizAttemptRepository attemptRepository,
            IStrategyFactory strategyFactory,
            QuizCommandInvoker commandInvoker,
            IEventDispatcher eventDispatcher)
        {
            _quizRepository = quizRepository;
            _attemptRepository = attemptRepository;
            _strategyFactory = strategyFactory;
            _commandInvoker = commandInvoker;
            _eventDispatcher = eventDispatcher;
        }

        public async Task<Guid> StartQuizAsync(Guid studentId, Guid quizId)
        {
            // 1. Verify student enrolled
            if (!await _quizRepository.IsStudentEnrolledAsync(studentId, (await _quizRepository.GetByIdAsync(quizId))?.ClassroomId ?? Guid.Empty))
            {
                 // Note: getting quiz twice here, optimize in real app
                 // For now, let's get quiz first
            }
            
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null) throw new Exception("Quiz not found");

            if (!await _quizRepository.IsStudentEnrolledAsync(studentId, quiz.ClassroomId))
            {
                throw new Exception("Student is not enrolled in the classroom.");
            }

            // 2. Check availability and attempt limits
            var attemptsCount = await _attemptRepository.GetAttemptCountAsync(studentId, quizId);
            if (!quiz.CanStart(attemptsCount, out var reason))
            {
                throw new Exception($"Cannot start quiz: {reason}");
            }
            
            var attempt = new QuizAttempt(quizId, studentId);
            attempt.Start();
            
            await _attemptRepository.AddAsync(attempt);
            return attempt.Id;
        }

        public async Task SubmitQuizAsync(Guid attemptId, SubmitQuizDto submission)
        {
            // 0. Idempotency Check
            if (await _attemptRepository.HasCommandBeenProcessedAsync(submission.CommandId))
            {
                return; // Already processed, idempotent success
            }

            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null) throw new Exception("Attempt not found");
            
            // Rehydrate state
            attempt.LoadState();

            // Create command
            var command = new SubmitQuizCommand(attempt, submission);
            
            // Execute command
            await _commandInvoker.ExecuteCommandAsync(command);

            // Post-submission logic (Grading, etc.) orchestrated here
            // 1. Evaluate
            var scoringStrategy = _strategyFactory.GetScoringStrategy("Points"); 
            attempt.Evaluate(scoringStrategy);

            // 2. Generate feedback
            var feedbackStrategy = _strategyFactory.GetFeedbackStrategy("Standard");
            attempt.GenerateFeedback(feedbackStrategy);

            // 3. Save changes & Mark Command Processed (Atomic)
            var strategy = _attemptRepository.GetExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _attemptRepository.BeginTransactionAsync();
                try 
                {
                    await _attemptRepository.UpdateAsync(attempt);
                    await _attemptRepository.MarkCommandAsProcessedAsync(submission.CommandId, "SubmitQuiz");
                    await _attemptRepository.CommitTransactionAsync(transaction);
                    
                    // 4. Dispatch events - Done after commit to ensure consistency
                    // If dispatch fails, we might have inconsistency between DB and Event Bus. 
                     // Outbox pattern is better here but for now this is "safer" than before.
                }
                catch
                {
                    await _attemptRepository.RollbackTransactionAsync(transaction);
                    throw;
                }
            });

             // 4. Dispatch events (After commit)
            var gradedEvent = new QuizAttemptGradedEvent(attempt.Id, attempt.StudentId, attempt.QuizId, attempt.TotalScore ?? 0);
            await _eventDispatcher.DispatchAsync(gradedEvent);
        }

        public async Task<QuizAttempt> GetReviewAsync(Guid attemptId)
        {
             var attempt = await _attemptRepository.GetByIdAsync(attemptId);
             if (attempt == null) throw new Exception("Attempt not found");
             return attempt;
        }
    }
}
