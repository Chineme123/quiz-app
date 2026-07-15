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

        public async Task<AttemptResultDto> SubmitQuizAsync(Guid attemptId, SubmitQuizDto submission)
        {
            // 0. Idempotency — an EXACT CommandId replay already ran to completion. Return the
            // existing result rather than re-running the submit. (Guard unchanged; it now hands
            // back the same result a first call returns, so an exact retry is a clean success.)
            if (await _attemptRepository.HasCommandBeenProcessedAsync(submission.CommandId))
            {
                return await LoadResultAsync(attemptId);
            }

            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null) throw new Exception("Attempt not found");

            // Rehydrate state
            attempt.LoadState();

            // Only an InProgress attempt is actually submitted by the command. Once it is
            // Submitted/Graded/Reviewable the command is a deliberate no-op — the case here is
            // a resubmit with a FRESH CommandId (an ordinary client retry after a network
            // hiccup) that slips past the exact-match guard above. Capture the distinction now,
            // before the command runs, so we can tell "just graded" from "already graded".
            var wasInProgress = attempt.CurrentStateName == "InProgress";

            // Create command
            var command = new SubmitQuizCommand(attempt, submission);

            // Execute command
            await _commandInvoker.ExecuteCommandAsync(command);

            // Post-submission logic (Grading, etc.) orchestrated here.
            // Load the quiz's questions — they carry the correct answers scoring/feedback need,
            // and back the result mapping on both paths below.
            var quiz = await _quizRepository.GetByIdAsync(attempt.QuizId)
                ?? throw new InvalidOperationException("Quiz not found for this attempt.");
            var questions = quiz.Questions.ToList();

            if (!wasInProgress)
            {
                // No-op resubmit: the command did nothing because the attempt was already
                // Submitted/Graded/Reviewable. Evaluating from a terminal (Graded/Reviewable)
                // state throws InvalidOperationException — which the controller surfaces as a
                // 400 — even though the command layer treats a resubmit as a safe no-op. So
                // skip Evaluate and the graded-event re-dispatch, and return the existing
                // graded result unchanged (the same DTO a poll of the attempt would return).
                return MapToResult(attempt, questions);
            }

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

            return MapToResult(attempt, questions);
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
            var questions = quiz?.Questions.ToList() ?? new List<Question>();
            return MapToResult(attempt, questions);
        }

        /// <summary>
        /// Loads an attempt with its quiz's questions and maps to the result DTO. The submit
        /// idempotency paths use this to hand back the existing result without re-grading.
        /// No ownership scoping here — submit already operates on the attempt by id, and the
        /// caller-scoped read (with the 404-on-mismatch check) is <see cref="GetResultAsync"/>.
        /// </summary>
        private async Task<AttemptResultDto> LoadResultAsync(Guid attemptId)
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null) throw new InvalidOperationException("Attempt not found for a processed command.");

            var quiz = await _quizRepository.GetByIdAsync(attempt.QuizId);
            var questions = quiz?.Questions.ToList() ?? new List<Question>();
            return MapToResult(attempt, questions);
        }

        /// <summary>
        /// The single attempt -> result DTO mapping, shared by submit and <see cref="GetResultAsync"/>
        /// so the resubmit no-op path returns exactly what the happy path (and a later poll of the
        /// attempt) returns. Manual, explicit mapping per code-standards §4.
        /// </summary>
        private static AttemptResultDto MapToResult(QuizAttempt attempt, IReadOnlyList<Question> questions)
        {
            var questionsById = questions.ToDictionary(q => q.Id);
            return new AttemptResultDto
            {
                AttemptId = attempt.Id,
                QuizId = attempt.QuizId,
                TotalScore = attempt.TotalScore,
                FeedbackStatus = attempt.FeedbackStatus.ToString(),
                Status = attempt.CurrentStateName,
                Answers = attempt.Answers.Select(a =>
                {
                    questionsById.TryGetValue(a.QuestionId, out var question);
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
