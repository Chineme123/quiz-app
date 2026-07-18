using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Enums;
using Quiztin.Modules.Assessment.Domain.Interfaces;
using Quiztin.Modules.Assessment.Domain.Factories;
using Quiztin.Modules.Assessment.Application.Commands;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Invokers;
using Quiztin.Modules.Assessment.Application.Results;
using Quiztin.Modules.Assessment.Domain.Events;
using Quiztin.Modules.Assessment.Domain.Observers; // Assuming existing or we need to add dispatch logic

namespace Quiztin.Modules.Assessment.Application.Facades
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

        /// <summary>Default and maximum page sizes for the available list (spec 0006, AC-1).</summary>
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 50;

        /// <summary>
        /// The quizzes this student may take, one page at a time (spec 0006, AC-1, AC-2). The
        /// repository scopes by enrolment, so this can never surface another classroom's work.
        /// Each row carries the single action that makes sense for it, and the open attempt id
        /// when there is one, so Resume needs no second call and cannot start a duplicate.
        /// </summary>
        public async Task<AvailableQuizzesDto> GetAvailableQuizzesAsync(Guid studentId, int page, int pageSize)
        {
            // Clamp rather than trust: a caller asking for 10,000 rows gets the cap, not the
            // database. Pagination is not optional even while the lists are small.
            var size = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);
            var pageNumber = Math.Max(page, 1);

            var (quizzes, total) = await _quizRepository.GetAvailableForStudentAsync(
                studentId, (pageNumber - 1) * size, size);

            var quizIds = quizzes.Select(q => q.Id).ToList();
            var attempts = quizIds.Count == 0
                ? new List<QuizAttempt>()
                : (await _attemptRepository.GetAttemptsForQuizzesAsync(studentId, quizIds)).ToList();

            return new AvailableQuizzesDto
            {
                Total = total,
                Page = pageNumber,
                PageSize = size,
                Items = quizzes.Select(q =>
                {
                    var mine = attempts.Where(a => a.QuizId == q.Id).ToList();
                    // A running attempt wins: the student is mid quiz, so Resume is the only
                    // action that makes sense. Otherwise a finished one means a result exists.
                    var running = mine.FirstOrDefault(a => a.CurrentStateName == "InProgress");
                    var finished = mine.FirstOrDefault(a =>
                        a.CurrentStateName is "Submitted" or "Graded" or "Reviewable");

                    return new AvailableQuizDto
                    {
                        QuizId = q.Id,
                        Title = q.Title,
                        DurationMinutes = q.DurationMinutes,
                        QuestionCount = q.Questions.Count,
                        State = running != null ? "InProgress" : finished != null ? "Graded" : "NotStarted",
                        AttemptId = running?.Id ?? finished?.Id
                    };
                }).ToList()
            };
        }

        public async Task<Guid> StartQuizAsync(Guid studentId, Guid quizId)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null) throw new Exception("Quiz not found");

            // Enrolment gates taking (FR7). Scoped from the caller's token, never a client id.
            if (!await _quizRepository.IsStudentEnrolledAsync(studentId, quiz.ClassroomId))
            {
                throw new Exception("Student is not enrolled in the classroom.");
            }

            // The availability window gates STARTING (foundation §69 trigger 2, spec 0006
            // AC-14): CanStart refuses outside it. A student already inside a quiz is left
            // alone to finish, which is why nothing re-checks the window on save or submit.
            // Only attempts that actually spent a try count here: submitted/graded ones, plus
            // any abandoned by a restart. An attempt abandoned by a quit does not (AC-15).
            var attemptsCount = await _attemptRepository.GetConsumedAttemptCountAsync(studentId, quizId);
            if (!quiz.CanStart(attemptsCount, out var reason))
            {
                throw new Exception($"Cannot start quiz: {reason}");
            }

            // Trigger 4, superseded: starting a new attempt abandons a prior unfinished one,
            // which is the one-active-attempt rule. It is recorded as Superseded so it counts
            // against MaxAttempts above, otherwise a student could restart forever and read
            // the questions for free (AC-15).
            var running = await _attemptRepository.GetInProgressAttemptAsync(studentId, quizId);
            if (running != null)
            {
                running.LoadState();
                running.Abandon(AbandonReason.Superseded);
                await _attemptRepository.UpdateAsync(running);
            }

            var attempt = new QuizAttempt(quizId, studentId);
            // The deadline is pinned from the quiz's duration at this instant (AC-3).
            attempt.Start(quiz.DurationMinutes);

            await _attemptRepository.AddAsync(attempt);
            return attempt.Id;
        }

        public async Task<SubmitQuizResult> SubmitQuizAsync(Guid attemptId, Guid studentId, SubmitQuizDto submission)
        {
            // Ownership scoping is a security boundary, not a nicety (code-standards §5,
            // security.md §4/§7): load the attempt and reject it when it isn't the caller's.
            // NotFound so the controller answers 404 whether the attempt is missing OR
            // someone else's — a non-owner never learns another student's attempt exists
            // (mirrors GetResultAsync, spec 0005 AC-9). Identity is the caller's Guid from the
            // token, never a client-supplied id. Checked before the idempotency guard below, so
            // no path can act on — or reveal — an attempt the caller doesn't own.
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null || attempt.StudentId != studentId) return SubmitQuizResult.NotFound();

            // 0. Idempotency — an EXACT CommandId replay already ran to completion. Return the
            // existing result rather than re-running the submit. (Guard unchanged; it now hands
            // back the same result a first call returns, so an exact retry is a clean success.)
            if (await _attemptRepository.HasCommandBeenProcessedAsync(submission.CommandId))
            {
                return SubmitQuizResult.Ok(await LoadResultAsync(attempt));
            }

            // Rehydrate state
            attempt.LoadState();

            // A superseded attempt has no result and can never gain one, so say so plainly
            // instead of letting the domain throw out of Submit as a raw 400. The real case is
            // a stale tab auto submitting at its countdown's zero after the student restarted
            // the quiz somewhere else (spec 0006, AC-16).
            if (attempt.CurrentStateName == "Abandoned") return SubmitQuizResult.Superseded();

            // Only an InProgress attempt is actually submitted by the command. Once it is
            // Submitted/Graded/Reviewable the command is a deliberate no-op — the case here is
            // a resubmit with a FRESH CommandId (an ordinary client retry after a network
            // hiccup) that slips past the exact-match guard above. Capture the distinction now,
            // before the command runs, so we can tell "just graded" from "already graded".
            var wasInProgress = attempt.CurrentStateName == "InProgress";

            // The command carries no answers: it grades the drafts already saved (AC-11). A
            // submit arriving after ExpiresAt is graded, not refused, because the drafts stopped
            // being writable at the deadline, so what is graded is exactly what the student had
            // saved when their time ran out (AC-12).
            var command = new SubmitQuizCommand(attempt);

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
                return SubmitQuizResult.Ok(MapToResult(attempt, questions));
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

            return SubmitQuizResult.Ok(MapToResult(attempt, questions));
        }

        /// <summary>
        /// The take screen's questions, read through the attempt (spec 0006, AC-4). The attempt
        /// is the authorisation: it is already owned by one student and enrolment was checked
        /// when it started, so this never touches the unscoped GET /api/quizzes/{id}.
        /// Returns null when the attempt is missing or not the caller's, so the controller
        /// answers 404 either way (AC-5). Correct answers never cross this boundary.
        /// </summary>
        public async Task<AttemptQuestionsDto?> GetAttemptQuestionsAsync(Guid attemptId, Guid studentId)
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null || attempt.StudentId != studentId) return null;

            var quiz = await _quizRepository.GetByIdAsync(attempt.QuizId);
            if (quiz == null) return null;

            return new AttemptQuestionsDto
            {
                AttemptId = attempt.Id,
                QuizTitle = quiz.Title,
                Status = attempt.CurrentStateName,
                ExpiresAt = attempt.ExpiresAt,
                // The client counts down on the gap between these two, not on its own clock,
                // so a device with a wrong clock still shows the true time left (AC-10).
                ServerNow = DateTime.UtcNow,
                DraftAnswers = new Dictionary<Guid, string>(attempt.DraftAnswers),
                Questions = quiz.Questions.Select(q => new AttemptQuestionDto
                {
                    Id = q.Id,
                    QuestionType = q.GetType().Name,
                    Prompt = q.Prompt,
                    Points = q.Points,
                    // Only a multiple choice question has options; nothing here reveals which
                    // one is right.
                    Options = (q as MultipleChoiceQuestion)?.Options.ToList()
                }).ToList()
            };
        }

        /// <summary>
        /// Replaces the whole saved draft set (spec 0006, AC-6, AC-7). Scoped to the caller like
        /// every other attempt endpoint. Rejected once the attempt is not running or its time is
        /// up, which is what makes the deadline server enforced rather than merely counted down
        /// by the client. The availability window is deliberately NOT re-checked here: a student
        /// already inside a quiz is left to finish (AC-14).
        /// </summary>
        public async Task<SaveDraftOutcome> SaveDraftAnswersAsync(Guid attemptId, Guid studentId, SaveDraftAnswersDto draft)
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null || attempt.StudentId != studentId) return SaveDraftOutcome.NotFound;

            attempt.LoadState();
            try
            {
                attempt.SaveDraftAnswers(draft.Answers, DateTime.UtcNow);
            }
            catch (InvalidOperationException)
            {
                // The entity guards both cases (wrong state, past the deadline). Either way the
                // answer is the same to the client: this attempt will not take more writes.
                return SaveDraftOutcome.Rejected;
            }

            await _attemptRepository.UpdateAsync(attempt);
            return SaveDraftOutcome.Saved;
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
        /// Maps an already-owner-checked attempt to the result DTO, loading its quiz's
        /// questions for the per-question breakdown. The exact-CommandId idempotency replay
        /// uses this to hand back the existing result without re-grading. Ownership was already
        /// enforced by the caller (<see cref="SubmitQuizAsync"/>); the caller-scoped read path
        /// is <see cref="GetResultAsync"/>.
        /// </summary>
        private async Task<AttemptResultDto> LoadResultAsync(QuizAttempt attempt)
        {
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
