using System;
using System.Linq;
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

            // Post-submission logic (Grading, etc.) orchestrated here.
            // Load the quiz's questions — they carry the correct answers scoring/feedback need.
            var quiz = await _quizRepository.GetByIdAsync(attempt.QuizId)
                ?? throw new InvalidOperationException("Quiz not found for this attempt.");
            var questions = quiz.Questions.ToList();

            // 1. Evaluate. Submit grades and returns fast; it does NOT call the model.
            // The attempt ends Graded with the score set and feedback Pending; the
            // graded event (below) drives feedback generation in the background (AC-1).
            var scoringStrategy = _strategyFactory.GetScoringStrategy("Points");
            attempt.Evaluate(scoringStrategy, questions);

            // 2. Save changes & Mark Command Processed (atomic + retriable).
            // Event dispatch stays AFTER the commit (accepted at-least-once seam,
            // no outbox in v1 — see foundation.md §9/§10).
            await _attemptRepository.ExecuteInTransactionAsync(async () =>
            {
                await _attemptRepository.UpdateAsync(attempt);
                await _attemptRepository.MarkCommandAsProcessedAsync(submission.CommandId, "SubmitQuiz");
            });

             // 4. Dispatch events (After commit)
            var gradedEvent = new QuizAttemptGradedEvent(attempt.Id, attempt.StudentId, attempt.QuizId, attempt.TotalScore ?? 0);
            await _eventDispatcher.DispatchAsync(gradedEvent);
        }

        /// <summary>
        /// A student's own attempt result, scoped to the caller (spec 0005, AC-8, AC-9).
        /// Returns null when the attempt does not exist OR is not the caller's, so the
        /// controller answers 404 either way and never reveals another student's work
        /// (security.md §7). Identity is the caller's Guid, never a client supplied id.
        /// </summary>
        public async Task<AttemptResultDto?> GetResultAsync(Guid attemptId, Guid studentId)
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null || attempt.StudentId != studentId) return null;

            var quiz = await _quizRepository.GetByIdAsync(attempt.QuizId);
            var questions = quiz?.Questions.ToDictionary(q => q.Id) ?? new Dictionary<Guid, Question>();

            return new AttemptResultDto
            {
                AttemptId = attempt.Id,
                QuizId = attempt.QuizId,
                TotalScore = attempt.TotalScore,
                FeedbackStatus = attempt.FeedbackStatus.ToString(),
                Status = attempt.CurrentStateName,
                Answers = attempt.Answers.Select(a =>
                {
                    questions.TryGetValue(a.QuestionId, out var question);
                    return new AttemptAnswerResultDto
                    {
                        QuestionId = a.QuestionId,
                        QuestionText = question?.Prompt ?? string.Empty,
                        ProvidedAnswer = a.ProvidedAnswer,
                        CorrectAnswer = question?.GetCorrectAnswerText() ?? string.Empty,
                        IsCorrect = a.IsCorrect,
                        PointsAwarded = a.PointsAwarded,
                        Feedback = a.Feedback,
                        FeedbackSource = a.FeedbackSource?.ToString()
                    };
                }).ToList()
            };
        }
    }
}
