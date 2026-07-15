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
using QuizService.Domain.Entities;
using QuizService.Domain.Events;
using QuizService.Infrastructure.Factories;
using QuizService.Infrastructure.Persistence;

namespace QuizService.Tests
{
    /// <summary>
    /// Submit at the facade seam (spec 0005 code-review fixes), against a real Postgres
    /// (Testcontainers, per code-standards §10). Covers two properties:
    /// (1) Idempotency — the top-level guard only short-circuits on an EXACT CommandId match,
    /// so a resubmit with a FRESH CommandId (an ordinary client retry after a network hiccup)
    /// slips past it and reaches the command. The command no-ops once the attempt is
    /// Graded/Reviewable; the facade must then return the existing graded result rather than
    /// calling Evaluate from a terminal state, which throws InvalidOperationException and
    /// surfaces as a 400.
    /// (2) Ownership scoping — a submit against an attempt that isn't the caller's is rejected
    /// with null (-> 404 at the controller), like GetResultAsync, without grading or
    /// dispatching, so it never reveals or mutates another student's attempt.
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
            var (attemptId, questionId, studentId) = await SeedInProgressAttemptAsync();
            var dispatcher = new CountingEventDispatcher();
            var responses = new List<QuizAnswerDto> { new() { QuestionId = questionId, Answer = "1" } }; // correct -> 10

            // First submit: a real InProgress -> Graded run that dispatches the graded event.
            AttemptResultDto? first;
            await using (var ctx = NewContext())
            {
                first = await BuildFacade(ctx, dispatcher).SubmitQuizAsync(
                    attemptId, studentId, new SubmitQuizDto { CommandId = Guid.NewGuid(), Responses = responses });
            }
            Assert.NotNull(first); // the owner's own attempt — never the non-owner null/404 path
            Assert.Equal("Graded", first.Status);
            Assert.Equal(10m, first.TotalScore);
            Assert.Equal(1, dispatcher.DispatchCount);

            // Second submit with a DIFFERENT CommandId. It slips past the exact-CommandId
            // idempotency guard and reaches the command, which no-ops because the attempt is
            // already Graded. Pre-fix the facade then called Evaluate on a Graded attempt ->
            // InvalidOperationException -> 400; a throw here would fail this test. It must
            // instead return the existing graded result.
            AttemptResultDto? second;
            await using (var ctx = NewContext())
            {
                second = await BuildFacade(ctx, dispatcher).SubmitQuizAsync(
                    attemptId, studentId, new SubmitQuizDto { CommandId = Guid.NewGuid(), Responses = responses });
            }

            // Same graded result as the first call — not a re-grade, not a throw.
            Assert.NotNull(second);
            Assert.Equal("Graded", second.Status);
            Assert.Equal(first.TotalScore, second.TotalScore);
            Assert.Equal(attemptId, second.AttemptId);
            Assert.Single(second.Answers);
            Assert.True(second.Answers[0].IsCorrect);

            // The no-op path skipped the graded-event re-dispatch: still exactly one dispatch.
            Assert.Equal(1, dispatcher.DispatchCount);

            // And the persisted attempt is untouched by the resubmit: still Graded, still 10.
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
            var (attemptId, questionId, studentId) = await SeedInProgressAttemptAsync();
            var dispatcher = new CountingEventDispatcher();
            var responses = new List<QuizAnswerDto> { new() { QuestionId = questionId, Answer = "1" } };
            var commandId = Guid.NewGuid();

            AttemptResultDto? first;
            await using (var ctx = NewContext())
            {
                first = await BuildFacade(ctx, dispatcher).SubmitQuizAsync(
                    attemptId, studentId, new SubmitQuizDto { CommandId = commandId, Responses = responses });
            }
            Assert.NotNull(first);

            // Same CommandId replay: short-circuits at the idempotency guard and returns the
            // existing result (the guard now hands back the DTO, not void).
            AttemptResultDto? second;
            await using (var ctx = NewContext())
            {
                second = await BuildFacade(ctx, dispatcher).SubmitQuizAsync(
                    attemptId, studentId, new SubmitQuizDto { CommandId = commandId, Responses = responses });
            }

            Assert.NotNull(second);
            Assert.Equal("Graded", second.Status);
            Assert.Equal(first.TotalScore, second.TotalScore);
            Assert.Equal(10m, second.TotalScore);
            // Guard short-circuits before the command/evaluate/dispatch — no second dispatch.
            Assert.Equal(1, dispatcher.DispatchCount);
        }

        [Fact]
        public async Task Submit_ByNonOwner_ReturnsNull_WithoutGradingOrDispatch()
        {
            // The attempt belongs to the seeded student; a DIFFERENT authenticated student
            // submits it by id. Ownership scoping (code-standards §5, security.md §4/§7) must
            // reject it exactly like GetResultAsync: return null so the controller answers 404
            // — never revealing that another student's attempt exists (spec 0005 AC-9) — and
            // never touching the victim's attempt.
            var (attemptId, questionId, ownerId) = await SeedInProgressAttemptAsync();
            var dispatcher = new CountingEventDispatcher();
            var responses = new List<QuizAnswerDto> { new() { QuestionId = questionId, Answer = "1" } };

            var nonOwner = Guid.NewGuid();
            Assert.NotEqual(ownerId, nonOwner);

            AttemptResultDto? result;
            await using (var ctx = NewContext())
            {
                result = await BuildFacade(ctx, dispatcher).SubmitQuizAsync(
                    attemptId, nonOwner, new SubmitQuizDto { CommandId = Guid.NewGuid(), Responses = responses });
            }

            // Rejected as if the attempt did not exist: null (-> 404 at the controller), and
            // nothing graded or dispatched — the ownership gate is before any state change.
            Assert.Null(result);
            Assert.Equal(0, dispatcher.DispatchCount);

            // The victim's attempt is untouched: still InProgress, still unscored.
            await using (var verify = NewContext())
            {
                var saved = await verify.QuizAttempts.FirstAsync(a => a.Id == attemptId);
                Assert.Equal("InProgress", saved.CurrentStateName);
                Assert.Null(saved.TotalScore);
            }
        }

        // Seeds a classroom, a published quiz with one graded question, and a started
        // (InProgress) attempt — the exact state the first submit expects. Returns the
        // attempt id, the question id the responses answer, and the owning student's id (the
        // caller a submit must be scoped to).
        private async Task<(Guid attemptId, Guid questionId, Guid studentId)> SeedInProgressAttemptAsync()
        {
            var classroom = new Classroom(_teacherId, "Idempotency Test Classroom");
            var quiz = new Quiz(classroom.Id, "Idempotency Test Quiz", 10, _teacherId) { IsPublished = true };
            var question = new MultipleChoiceQuestion("2 + 2?", 10, new List<string> { "3", "4", "5" }, 1);
            quiz.Questions.Add(question);
            _context.Classrooms.Add(classroom);
            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            var studentId = Guid.NewGuid();
            var attempt = new QuizAttempt(quiz.Id, studentId);
            attempt.Start();
            await new QuizAttemptRepository(_context).AddAsync(attempt);

            return (attempt.Id, question.Id, studentId);
        }

        // A fresh context per facade call, mirroring the per-request scope of the real app
        // (each submit is its own HTTP request with its own scoped DbContext).
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
