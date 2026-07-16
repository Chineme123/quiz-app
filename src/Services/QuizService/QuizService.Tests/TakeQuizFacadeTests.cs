using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;
using QuizService.Application.DTOs;
using QuizService.Application.Facades;
using QuizService.Application.Invokers;
using QuizService.Application.Results;
using QuizService.Domain.Entities;
using QuizService.Domain.Enums;
using QuizService.Domain.Events;
using QuizService.Infrastructure.Factories;
using QuizService.Infrastructure.Persistence;

namespace QuizService.Tests
{
    /// <summary>
    /// The take path at the facade seam, against a real Postgres (Testcontainers, per
    /// code-standards §10). Covers the properties that are easy to break and expensive to get
    /// wrong: idempotency, ownership scoping, the server enforced deadline, and the attempt
    /// limit rules that keep a one shot quiz honest (specs 0005 and 0006).
    /// Needs Docker (available in CI).
    /// </summary>
    public class TakeQuizFacadeTests : IAsyncLifetime
    {
#pragma warning disable CS0618 // the parameterless builder ctor is deprecated in 4.13; WithImage sets the image
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .Build();
#pragma warning restore CS0618

        private QuizDbContext _context = null!;
        private readonly Guid _teacherId = Guid.NewGuid();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            var options = new DbContextOptionsBuilder<QuizDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;
            _context = new QuizDbContext(options);
            await _context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
            await _postgres.DisposeAsync();
        }

        [Fact]
        public async Task Resubmit_WithFreshCommandId_ReturnsExistingResult_WithoutRegradeOrRedispatch()
        {
            var (attemptId, _, studentId, _) = await SeedInProgressAttemptAsync();
            var dispatcher = new CountingEventDispatcher();

            // First submit: a real InProgress -> Graded run that dispatches the graded event.
            // The body carries only a CommandId; the drafts already saved are what gets graded.
            SubmitQuizResult first;
            await using (var ctx = NewContext())
            {
                first = await BuildFacade(ctx, dispatcher).SubmitQuizAsync(
                    attemptId, studentId, new SubmitQuizDto { CommandId = Guid.NewGuid() });
            }
            Assert.Equal(SubmitQuizOutcome.Graded, first.Outcome);
            Assert.Equal("Graded", first.Result!.Status);
            Assert.Equal(10m, first.Result.TotalScore);
            Assert.Equal(1, dispatcher.DispatchCount);

            // Second submit with a DIFFERENT CommandId. It slips past the exact-CommandId
            // idempotency guard and reaches the command, which no-ops because the attempt is
            // already Graded. The facade must return the existing graded result rather than
            // calling Evaluate from a terminal state, which throws and surfaces as a 400.
            SubmitQuizResult second;
            await using (var ctx = NewContext())
            {
                second = await BuildFacade(ctx, dispatcher).SubmitQuizAsync(
                    attemptId, studentId, new SubmitQuizDto { CommandId = Guid.NewGuid() });
            }

            Assert.Equal(SubmitQuizOutcome.Graded, second.Outcome);
            Assert.Equal("Graded", second.Result!.Status);
            Assert.Equal(first.Result.TotalScore, second.Result.TotalScore);
            Assert.Single(second.Result.Answers);
            Assert.True(second.Result.Answers[0].IsCorrect);

            // The no-op path skipped the graded-event re-dispatch: still exactly one dispatch.
            Assert.Equal(1, dispatcher.DispatchCount);

            await using (var verify = NewContext())
            {
                var saved = await verify.QuizAttempts.FirstAsync(a => a.Id == attemptId);
                Assert.Equal("Graded", saved.CurrentStateName);
                Assert.Equal(10m, saved.TotalScore);
            }
        }

        [Fact]
        public async Task Resubmit_WithSameCommandId_ShortCircuits_AndReturnsExistingResult()
        {
            var (attemptId, _, studentId, _) = await SeedInProgressAttemptAsync();
            var dispatcher = new CountingEventDispatcher();
            var commandId = Guid.NewGuid();

            SubmitQuizResult first;
            await using (var ctx = NewContext())
            {
                first = await BuildFacade(ctx, dispatcher).SubmitQuizAsync(
                    attemptId, studentId, new SubmitQuizDto { CommandId = commandId });
            }
            Assert.Equal(SubmitQuizOutcome.Graded, first.Outcome);

            SubmitQuizResult second;
            await using (var ctx = NewContext())
            {
                second = await BuildFacade(ctx, dispatcher).SubmitQuizAsync(
                    attemptId, studentId, new SubmitQuizDto { CommandId = commandId });
            }

            Assert.Equal(SubmitQuizOutcome.Graded, second.Outcome);
            Assert.Equal(10m, second.Result!.TotalScore);
            // Guard short-circuits before the command/evaluate/dispatch — no second dispatch.
            Assert.Equal(1, dispatcher.DispatchCount);
        }

        [Fact]
        public async Task Submit_ByNonOwner_IsNotFound_WithoutGradingOrDispatch()
        {
            // Ownership scoping (code-standards §5, security.md §4/§7): a submit for someone
            // else's attempt is answered exactly like a missing one, so a non owner never
            // learns it exists (spec 0005 AC-9, spec 0006 AC-5).
            var (attemptId, _, ownerId, _) = await SeedInProgressAttemptAsync();
            var dispatcher = new CountingEventDispatcher();
            var nonOwner = Guid.NewGuid();
            Assert.NotEqual(ownerId, nonOwner);

            SubmitQuizResult result;
            await using (var ctx = NewContext())
            {
                result = await BuildFacade(ctx, dispatcher).SubmitQuizAsync(
                    attemptId, nonOwner, new SubmitQuizDto { CommandId = Guid.NewGuid() });
            }

            Assert.Equal(SubmitQuizOutcome.NotFound, result.Outcome);
            Assert.Null(result.Result);
            Assert.Equal(0, dispatcher.DispatchCount);

            // The victim's attempt is untouched: still InProgress, still unscored.
            await using (var verify = NewContext())
            {
                var saved = await verify.QuizAttempts.FirstAsync(a => a.Id == attemptId);
                Assert.Equal("InProgress", saved.CurrentStateName);
                Assert.Null(saved.TotalScore);
            }
        }

        [Fact]
        public async Task SaveDraft_ByNonOwner_IsNotFound()
        {
            var (attemptId, questionId, ownerId, _) = await SeedInProgressAttemptAsync();
            var nonOwner = Guid.NewGuid();
            Assert.NotEqual(ownerId, nonOwner);

            SaveDraftOutcome outcome;
            await using (var ctx = NewContext())
            {
                outcome = await BuildFacade(ctx, new CountingEventDispatcher()).SaveDraftAnswersAsync(
                    attemptId, nonOwner,
                    new SaveDraftAnswersDto { Answers = new Dictionary<Guid, string> { [questionId] = "0" } });
            }

            // Same answer as a missing attempt: a non owner cannot write to, or learn about,
            // another student's work (spec 0006, AC-5).
            Assert.Equal(SaveDraftOutcome.NotFound, outcome);
        }

        [Fact]
        public async Task SaveDraft_AfterTheDeadline_IsRejected()
        {
            // The deadline has to be enforced by the server, not merely counted down by the
            // client, or a student who blocks the timer just keeps working (spec 0006, AC-7).
            var (attemptId, questionId, studentId, _) = await SeedInProgressAttemptAsync(durationMinutes: -1);

            SaveDraftOutcome outcome;
            await using (var ctx = NewContext())
            {
                outcome = await BuildFacade(ctx, new CountingEventDispatcher()).SaveDraftAnswersAsync(
                    attemptId, studentId,
                    new SaveDraftAnswersDto { Answers = new Dictionary<Guid, string> { [questionId] = "0" } });
            }

            Assert.Equal(SaveDraftOutcome.Rejected, outcome);
        }

        [Fact]
        public async Task Submit_AfterTheDeadline_GradesTheSavedDrafts()
        {
            // Expiry grades rather than abandons (spec 0006, AC-12, overriding foundation §69
            // trigger 1). What is graded is what the student had saved when time ran out,
            // because drafts stopped being writable at the deadline.
            var (attemptId, _, studentId, _) = await SeedInProgressAttemptAsync(durationMinutes: -1);

            SubmitQuizResult result;
            await using (var ctx = NewContext())
            {
                result = await BuildFacade(ctx, new CountingEventDispatcher()).SubmitQuizAsync(
                    attemptId, studentId, new SubmitQuizDto { CommandId = Guid.NewGuid() });
            }

            Assert.Equal(SubmitQuizOutcome.Graded, result.Outcome);
            Assert.Equal(10m, result.Result!.TotalScore);
        }

        [Fact]
        public async Task Submit_OnASupersededAttempt_ReportsSuperseded_NotARawError()
        {
            // The real case: a stale tab auto submits at its countdown's zero after the student
            // restarted the quiz elsewhere. Without this the domain throws out of Submit and the
            // student sees a raw 400 (spec 0006, AC-16).
            var (attemptId, _, studentId, quizId) = await SeedInProgressAttemptAsync();

            await using (var ctx = NewContext())
            {
                var repo = new QuizAttemptRepository(ctx);
                var running = await repo.GetInProgressAttemptAsync(studentId, quizId);
                running!.Abandon(AbandonReason.Superseded);
                await repo.UpdateAsync(running);
            }

            SubmitQuizResult result;
            await using (var ctx = NewContext())
            {
                result = await BuildFacade(ctx, new CountingEventDispatcher()).SubmitQuizAsync(
                    attemptId, studentId, new SubmitQuizDto { CommandId = Guid.NewGuid() });
            }

            Assert.Equal(SubmitQuizOutcome.Superseded, result.Outcome);
            Assert.Null(result.Result);
        }

        [Fact]
        public async Task Restarting_AbandonsTheOldAttempt_AndConsumesTheAttemptLimit()
        {
            // The farming hole: if a superseded abandon cost nothing, a student could start,
            // read the questions, restart, and repeat forever on a MaxAttempts=1 quiz. Only a
            // restart is an abandon the student controls, so it is the one that pays (AC-15).
            var (firstAttemptId, _, studentId, quizId) = await SeedInProgressAttemptAsync();

            Guid secondAttemptId;
            await using (var ctx = NewContext())
            {
                secondAttemptId = await BuildFacade(ctx, new CountingEventDispatcher())
                    .StartQuizAsync(studentId, quizId);
            }
            Assert.NotEqual(firstAttemptId, secondAttemptId);

            await using (var verify = NewContext())
            {
                var old = await verify.QuizAttempts.FirstAsync(a => a.Id == firstAttemptId);
                Assert.Equal("Abandoned", old.CurrentStateName);
                Assert.Equal(AbandonReason.Superseded, old.AbandonReason);
            }

            // The quiz is MaxAttempts = 1 and the restart already spent it, so a third start
            // is refused rather than handing out another free look at the questions.
            await using (var ctx = NewContext())
            {
                var facade = BuildFacade(ctx, new CountingEventDispatcher());
                var thrown = await Record.ExceptionAsync(() => facade.StartQuizAsync(studentId, quizId));
                Assert.NotNull(thrown);
                Assert.Contains("Maximum attempts", thrown.Message);
            }
        }

        [Fact]
        public async Task GetAttemptQuestions_NeverReturnsTheCorrectAnswer_AndCarriesTheClockAndDrafts()
        {
            var (attemptId, questionId, studentId, _) = await SeedInProgressAttemptAsync();

            AttemptQuestionsDto? payload;
            await using (var ctx = NewContext())
            {
                payload = await BuildFacade(ctx, new CountingEventDispatcher())
                    .GetAttemptQuestionsAsync(attemptId, studentId);
            }

            Assert.NotNull(payload);
            var question = Assert.Single(payload!.Questions);
            Assert.Equal("MultipleChoiceQuestion", question.QuestionType);
            Assert.Equal(new List<string> { "3", "4", "5" }, question.Options);
            // The payload lives in a student's browser, so the whole point is what is NOT here:
            // nothing on it says which option is right (spec 0006, AC-4).
            Assert.DoesNotContain("Correct", string.Join(",", typeof(AttemptQuestionDto).GetProperties().Select(p => p.Name)));
            // The clock the client counts down on, and the work already saved (AC-9, AC-10).
            Assert.True(payload.ExpiresAt > payload.ServerNow);
            Assert.Equal("1", payload.DraftAnswers[questionId]);
        }

        [Fact]
        public async Task GetAttemptQuestions_ByNonOwner_IsNotFound()
        {
            var (attemptId, _, ownerId, _) = await SeedInProgressAttemptAsync();
            var nonOwner = Guid.NewGuid();
            Assert.NotEqual(ownerId, nonOwner);

            await using var ctx = NewContext();
            var payload = await BuildFacade(ctx, new CountingEventDispatcher())
                .GetAttemptQuestionsAsync(attemptId, nonOwner);

            Assert.Null(payload);
        }

        [Fact]
        public async Task AvailableQuizzes_ListsOnlyEnrolledPublishedQuizzes_WithTheRightAction()
        {
            var (_, _, studentId, quizId) = await SeedInProgressAttemptAsync();

            // A second classroom and quiz the student is NOT enrolled in: it must never appear.
            var otherClassroom = new Classroom(_teacherId, "Someone Else's Classroom");
            var otherQuiz = new Quiz(otherClassroom.Id, "Not Mine", 10, _teacherId) { IsPublished = true };
            _context.Classrooms.Add(otherClassroom);
            _context.Quizzes.Add(otherQuiz);
            // An unpublished quiz in the student's own classroom: also must not appear.
            var draftQuiz = new Quiz((await _context.Quizzes.FirstAsync(q => q.Id == quizId)).ClassroomId,
                "Unpublished", 10, _teacherId) { IsPublished = false };
            _context.Quizzes.Add(draftQuiz);
            await _context.SaveChangesAsync();

            AvailableQuizzesDto list;
            await using (var ctx = NewContext())
            {
                list = await BuildFacade(ctx, new CountingEventDispatcher())
                    .GetAvailableQuizzesAsync(studentId, page: 1, pageSize: 0);
            }

            var item = Assert.Single(list.Items);
            Assert.Equal(quizId, item.QuizId);
            Assert.Equal(1, list.Total);
            // The student has a running attempt, so the list offers Resume and hands over the
            // id, which is what stops the list starting a second attempt (AC-2).
            Assert.Equal("InProgress", item.State);
            Assert.NotNull(item.AttemptId);
            Assert.Equal(1, item.QuestionCount);
        }

        /// <summary>
        /// Seeds a classroom, an enrolment, a published one shot quiz with one question, and a
        /// started attempt whose correct answer is already saved as a draft. That is the state
        /// a submit now expects: the answers are on the server before submit, not in its body.
        /// A negative duration pins a deadline in the past, which is how the expiry tests get an
        /// already expired attempt without waiting.
        /// </summary>
        private async Task<(Guid attemptId, Guid questionId, Guid studentId, Guid quizId)> SeedInProgressAttemptAsync(
            int durationMinutes = 10)
        {
            var classroom = new Classroom(_teacherId, "Take Test Classroom");
            var quiz = new Quiz(classroom.Id, "Take Test Quiz", durationMinutes, _teacherId) { IsPublished = true };
            var question = new MultipleChoiceQuestion("2 + 2?", 10, new List<string> { "3", "4", "5" }, 1);
            quiz.Questions.Add(question);
            _context.Classrooms.Add(classroom);
            _context.Quizzes.Add(quiz);

            var studentId = Guid.NewGuid();
            _context.Enrollments.Add(new Enrollment(studentId, classroom.Id));
            await _context.SaveChangesAsync();

            var attempt = new QuizAttempt(quiz.Id, studentId);
            attempt.Start(durationMinutes);
            // Save as of just inside the deadline, not as of StartedAt: with a negative duration
            // the deadline is already behind StartedAt, so anchoring to it is what lets an
            // expired seed still carry the work the expiry tests expect to be graded.
            attempt.SaveDraftAnswers(
                new Dictionary<Guid, string> { [question.Id] = "1" }, attempt.ExpiresAt.AddSeconds(-1));
            await new QuizAttemptRepository(_context).AddAsync(attempt);

            return (attempt.Id, question.Id, studentId, quiz.Id);
        }

        // A fresh context per facade call, mirroring the per-request scope of the real app
        // (each call is its own HTTP request with its own scoped DbContext).
        private QuizDbContext NewContext() =>
            new(new DbContextOptionsBuilder<QuizDbContext>()
                .UseNpgsql(_postgres.GetConnectionString()).Options);

        private static TakeQuizFacade BuildFacade(QuizDbContext ctx, IEventDispatcher dispatcher) =>
            new(
                new QuizRepository(ctx),
                new QuizAttemptRepository(ctx),
                new StrategyFactory(),
                new QuizCommandInvoker(),
                dispatcher);

        // Counts graded-event dispatches so a test can prove the no-op resubmit path does
        // NOT re-dispatch (the happy path dispatches exactly once).
        private sealed class CountingEventDispatcher : IEventDispatcher
        {
            public int DispatchCount { get; private set; }

            public Task DispatchAsync<TEvent>(TEvent domainEvent)
            {
                DispatchCount++;
                return Task.CompletedTask;
            }
        }
    }
}
