using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Results;
using Quiztin.Modules.Assessment.Application.Services;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Interfaces;
using Quiztin.Modules.Assessment.Infrastructure.Configuration;
using Quiztin.Modules.Assessment.Infrastructure.Persistence;
using Quiztin.Modules.Assessment.Infrastructure.Strategies;

namespace Quiztin.Modules.Assessment.Tests
{
    /// <summary>
    /// AI generation drafts plus the accept and discard flow (spec 0009 task 3, AC-4, AC-8, AC-9,
    /// AC-10) against a real Postgres: the batch persists as jsonb, one row per quiz is a database
    /// invariant (unique QuizId), and the take-path lock and owner scoping are real. The model call
    /// is stubbed by a fake strategy; the parser's own tolerant behavior is covered separately by
    /// GeneratedCandidateParserTests. Needs Docker.
    /// </summary>
    public class QuizGenerationTests : IAsyncLifetime
    {
#pragma warning disable CS0618 // the parameterless builder ctor is deprecated in 4.13; WithImage sets the image
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .Build();
#pragma warning restore CS0618

        private QuizDbContext _context = null!;
        private readonly Guid _teacherId = Guid.NewGuid();
        private readonly Guid _studentId = Guid.NewGuid();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            _context = NewContext();
            await _context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
            await _postgres.DisposeAsync();
        }

        [Fact]
        public async Task Generating_with_the_template_fallback_stores_the_requested_number_of_empty_drafts()
        {
            var quizId = await SeedQuizAsync();

            var result = await Service(NewContext(), Templates()).GenerateQuestionsAsync(quizId, _teacherId,
                new GenerateQuestionsDto { Topic = "Fractions", Difficulty = "easy", Count = 3 });

            Assert.Equal(GenerationOutcome.Ok, result.Outcome);
            Assert.Equal(3, result.Draft!.Candidates.Count);

            // The batch persisted as one jsonb row and reads back.
            var read = await Service(NewContext(), Templates()).GetDraftsAsync(quizId, _teacherId);
            Assert.Equal(3, read!.Candidates.Count);
            Assert.Single(await NewContext().GeneratedQuestionDrafts.Where(d => d.QuizId == quizId).ToListAsync());
        }

        [Fact]
        public async Task Regenerating_replaces_the_one_batch_rather_than_adding_a_second()
        {
            var quizId = await SeedQuizAsync();

            await Service(NewContext(), Fake(ValidMultipleChoice("first"))).GenerateQuestionsAsync(quizId, _teacherId, Request());
            await Service(NewContext(), Fake(ValidMultipleChoice("second"))).GenerateQuestionsAsync(quizId, _teacherId, Request());

            Assert.Single(await NewContext().GeneratedQuestionDrafts.Where(d => d.QuizId == quizId).ToListAsync());
            var read = await Service(NewContext(), Templates()).GetDraftsAsync(quizId, _teacherId);
            Assert.Equal("second", read!.Candidates.Single().Prompt);
        }

        [Fact]
        public async Task Accepting_promotes_the_chosen_candidates_and_clears_the_batch()
        {
            var quizId = await SeedQuizAsync();
            var gen = await Service(NewContext(), Fake(ValidMultipleChoice("2+2?"), ValidTrueFalse("Sky blue?")))
                .GenerateQuestionsAsync(quizId, _teacherId, Request());
            var ids = gen.Draft!.Candidates.Select(c => c.Id).ToList();

            var accept = await Service(NewContext(), Templates()).AcceptDraftsAsync(quizId, _teacherId,
                new AcceptDraftsDto { DraftIds = ids });

            Assert.Equal(AuthoringOutcome.Ok, accept.Outcome);
            // Both candidates are now real questions on the quiz, and the batch is gone (AC-8).
            Assert.Equal(2, await NewContext().Questions.CountAsync(q => q.QuizId == quizId));
            Assert.Empty(await NewContext().GeneratedQuestionDrafts.Where(d => d.QuizId == quizId).ToListAsync());
        }

        [Fact]
        public async Task Accepting_an_unfilled_template_is_rejected_and_the_batch_stays()
        {
            var quizId = await SeedQuizAsync();
            var gen = await Service(NewContext(), Templates()).GenerateQuestionsAsync(quizId, _teacherId,
                new GenerateQuestionsDto { Topic = "Anything", Count = 1 });
            var ids = gen.Draft!.Candidates.Select(c => c.Id).ToList();

            var accept = await Service(NewContext(), Templates()).AcceptDraftsAsync(quizId, _teacherId,
                new AcceptDraftsDto { DraftIds = ids });

            Assert.Equal(AuthoringOutcome.Invalid, accept.Outcome);
            Assert.Empty(await NewContext().Questions.Where(q => q.QuizId == quizId).ToListAsync());
            Assert.Single(await NewContext().GeneratedQuestionDrafts.Where(d => d.QuizId == quizId).ToListAsync());
        }

        [Fact]
        public async Task Discarding_clears_the_batch_and_is_idempotent()
        {
            var quizId = await SeedQuizAsync();
            await Service(NewContext(), Fake(ValidTrueFalse("P"))).GenerateQuestionsAsync(quizId, _teacherId, Request());

            Assert.Equal(AuthoringOutcome.Ok, (await Service(NewContext(), Templates()).DiscardDraftsAsync(quizId, _teacherId)).Outcome);
            Assert.Empty(await NewContext().GeneratedQuestionDrafts.Where(d => d.QuizId == quizId).ToListAsync());
            // Discarding again is safe.
            Assert.Equal(AuthoringOutcome.Ok, (await Service(NewContext(), Templates()).DiscardDraftsAsync(quizId, _teacherId)).Outcome);
        }

        [Fact]
        public async Task Once_a_quiz_has_an_attempt_generate_and_accept_lock()
        {
            var quizId = await SeedQuizAsync();
            await Service(NewContext(), Fake(ValidTrueFalse("P"))).GenerateQuestionsAsync(quizId, _teacherId, Request());
            await SeedAttemptAsync(quizId);

            var gen = await Service(NewContext(), Fake(ValidTrueFalse("Q"))).GenerateQuestionsAsync(quizId, _teacherId, Request());
            Assert.Equal(GenerationOutcome.Locked, gen.Outcome);

            var accept = await Service(NewContext(), Templates()).AcceptDraftsAsync(quizId, _teacherId,
                new AcceptDraftsDto { DraftIds = new List<Guid> { Guid.NewGuid() } });
            Assert.Equal(AuthoringOutcome.Locked, accept.Outcome);
        }

        [Fact]
        public async Task A_blank_topic_is_rejected()
        {
            var quizId = await SeedQuizAsync();
            var result = await Service(NewContext(), Templates()).GenerateQuestionsAsync(quizId, _teacherId,
                new GenerateQuestionsDto { Topic = "  ", Count = 3 });
            Assert.Equal(GenerationOutcome.Invalid, result.Outcome);
        }

        [Fact]
        public async Task A_non_owner_gets_not_found_across_generate_read_accept_and_discard()
        {
            var quizId = await SeedQuizAsync();
            await Service(NewContext(), Fake(ValidTrueFalse("P"))).GenerateQuestionsAsync(quizId, _teacherId, Request());
            var stranger = Guid.NewGuid();

            Assert.Equal(GenerationOutcome.NotFound, (await Service(NewContext(), Templates()).GenerateQuestionsAsync(quizId, stranger, Request())).Outcome);
            Assert.Null(await Service(NewContext(), Templates()).GetDraftsAsync(quizId, stranger));
            Assert.Equal(AuthoringOutcome.NotFound, (await Service(NewContext(), Templates()).AcceptDraftsAsync(quizId, stranger, new AcceptDraftsDto())).Outcome);
            Assert.Equal(AuthoringOutcome.NotFound, (await Service(NewContext(), Templates()).DiscardDraftsAsync(quizId, stranger)).Outcome);
            // The batch is untouched.
            Assert.Single(await NewContext().GeneratedQuestionDrafts.Where(d => d.QuizId == quizId).ToListAsync());
        }

        // ---- helpers ----

        private static GenerateQuestionsDto Request() => new() { Topic = "Topic", Difficulty = "medium", Count = 1 };

        private static GeneratedCandidate ValidMultipleChoice(string prompt) => new()
        {
            QuestionType = "MultipleChoice",
            Prompt = prompt,
            Points = 5,
            Options = new List<string> { "a", "b" },
            CorrectOptionIndex = 1
        };

        private static GeneratedCandidate ValidTrueFalse(string prompt) => new()
        {
            QuestionType = "TrueFalse",
            Prompt = prompt,
            Points = 2,
            CorrectAnswerBool = true
        };

        private async Task<Guid> SeedQuizAsync()
        {
            var classroom = new Classroom(_teacherId, "Class");
            var quiz = new Quiz(classroom.Id, "Quiz", 10, _teacherId);
            await using var ctx = NewContext();
            ctx.Classrooms.Add(classroom);
            ctx.Quizzes.Add(quiz);
            await ctx.SaveChangesAsync();
            return quiz.Id;
        }

        private async Task SeedAttemptAsync(Guid quizId)
        {
            await using var ctx = NewContext();
            var attempt = new QuizAttempt(quizId, _studentId);
            attempt.Start(10);
            ctx.QuizAttempts.Add(attempt);
            await ctx.SaveChangesAsync();
        }

        private QuizDbContext NewContext() =>
            new(new DbContextOptionsBuilder<QuizDbContext>()
                .UseNpgsql(_postgres.GetConnectionString()).Options);

        private static QuizAppService Service(QuizDbContext ctx, IQuestionGenerationStrategy strategy) =>
            new(new QuizRepository(ctx), new QuizAttemptRepository(ctx),
                new GeneratedQuestionDraftRepository(ctx), strategy);

        private static IQuestionGenerationStrategy Templates() =>
            new TemplateQuestionGenerationStrategy(Options.Create(new GenerationOptions()));

        private static IQuestionGenerationStrategy Fake(params GeneratedCandidate[] candidates) =>
            new FakeStrategy(candidates);

        private sealed class FakeStrategy : IQuestionGenerationStrategy
        {
            private readonly IReadOnlyList<GeneratedCandidate> _candidates;
            public FakeStrategy(IReadOnlyList<GeneratedCandidate> candidates) => _candidates = candidates;
            public Task<IReadOnlyList<GeneratedCandidate>> GenerateAsync(string topic, string difficulty, int count)
                => Task.FromResult(_candidates);
        }
    }
}
