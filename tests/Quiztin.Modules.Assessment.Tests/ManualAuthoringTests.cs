using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Results;
using Microsoft.Extensions.Options;
using Quiztin.Modules.Assessment.Application.Services;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Infrastructure.Configuration;
using Quiztin.Modules.Assessment.Infrastructure.Parsing;
using Quiztin.Modules.Assessment.Infrastructure.Persistence;
using Quiztin.Modules.Assessment.Infrastructure.Strategies;

namespace Quiztin.Modules.Assessment.Tests
{
    /// <summary>
    /// Manual quiz authoring (spec 0009 task 2, AC-3, AC-9, AC-10) against a real Postgres
    /// (Testcontainers, per code-standards §10, not a substitute provider): edit and delete are
    /// EF graph mutations that only behave the same as production on the real provider, and the
    /// lock on attempt rule reads a real attempt row. Needs Docker (available in CI).
    /// </summary>
    public class ManualAuthoringTests : IAsyncLifetime
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
        public async Task Editing_a_question_updates_it_in_place()
        {
            var (_, quizId) = await SeedQuizAsync();
            var questionId = await FirstQuestionIdAsync(quizId);

            var result = await NewService(NewContext()).EditQuestionAsync(quizId, questionId, _teacherId,
                new AddQuestionDto
                {
                    QuestionType = "MultipleChoice",
                    Prompt = "3 + 3?",
                    Points = 9,
                    Options = new List<string> { "5", "6", "7" },
                    CorrectOptionIndex = 1
                });

            Assert.Equal(AuthoringOutcome.Ok, result.Outcome);

            var stored = (MultipleChoiceQuestion)await NewContext().Questions.FirstAsync(q => q.Id == questionId);
            Assert.Equal("3 + 3?", stored.Prompt);
            Assert.Equal(9, stored.Points);
            Assert.Equal(1, stored.CorrectOptionIndex);
            Assert.Equal(new List<string> { "5", "6", "7" }, stored.Options);
            // The same row was edited, not replaced.
            Assert.Single(await QuestionsOfAsync(quizId));
        }

        [Fact]
        public async Task Deleting_a_question_removes_only_that_row()
        {
            var (_, quizId) = await SeedQuizAsync();
            await NewService(NewContext()).AddQuestionAsync(quizId, _teacherId, new AddQuestionDto
            {
                QuestionType = "TrueFalse", Prompt = "True or false?", Points = 1, CorrectAnswerBool = true
            });
            var toDelete = (await NewContext().Questions
                .FirstAsync(q => q.QuizId == quizId && q.QuestionType == nameof(MultipleChoiceQuestion))).Id;

            var result = await NewService(NewContext()).DeleteQuestionAsync(quizId, toDelete, _teacherId);

            Assert.Equal(AuthoringOutcome.Ok, result.Outcome);
            var remaining = await QuestionsOfAsync(quizId);
            Assert.Single(remaining);
            Assert.Equal(nameof(TrueFalseQuestion), remaining[0].QuestionType);
        }

        [Fact]
        public async Task Once_a_quiz_has_an_attempt_add_edit_and_delete_all_lock()
        {
            var (_, quizId) = await SeedQuizAsync(withAttempt: true);
            var questionId = await FirstQuestionIdAsync(quizId);

            var add = await NewService(NewContext()).AddQuestionAsync(quizId, _teacherId, new AddQuestionDto
            {
                QuestionType = "TrueFalse", Prompt = "Late add?", Points = 1, CorrectAnswerBool = false
            });
            var edit = await NewService(NewContext()).EditQuestionAsync(quizId, questionId, _teacherId, new AddQuestionDto
            {
                QuestionType = "MultipleChoice", Prompt = "changed", Points = 2,
                Options = new List<string> { "a", "b" }, CorrectOptionIndex = 0
            });
            var delete = await NewService(NewContext()).DeleteQuestionAsync(quizId, questionId, _teacherId);

            Assert.Equal(AuthoringOutcome.Locked, add.Outcome);
            Assert.Equal(AuthoringOutcome.Locked, edit.Outcome);
            Assert.Equal(AuthoringOutcome.Locked, delete.Outcome);
            // Nothing changed: still exactly the one seeded question.
            Assert.Single(await QuestionsOfAsync(quizId));
        }

        [Fact]
        public async Task The_detail_read_reports_the_locked_flag()
        {
            var (_, quizId) = await SeedQuizAsync();

            var unlocked = await NewService(NewContext()).GetQuizForEditingAsync(quizId, _teacherId);
            Assert.NotNull(unlocked);
            Assert.False(unlocked!.IsLocked);
            Assert.Single(unlocked.Questions);

            await SeedAttemptAsync(quizId);

            var locked = await NewService(NewContext()).GetQuizForEditingAsync(quizId, _teacherId);
            Assert.True(locked!.IsLocked);
        }

        [Fact]
        public async Task The_class_quiz_list_carries_counts_and_is_owner_scoped()
        {
            var (classroomId, quizId) = await SeedQuizAsync(withAttempt: true); // one question, one attempt
            await NewService(NewContext()).CreateQuizAsync(classroomId, _teacherId,
                new CreateQuizDto { Title = "Empty Quiz", DurationMinutes = 5 });

            var list = await NewService(NewContext()).GetQuizzesForClassroomAsync(classroomId, _teacherId);

            Assert.NotNull(list);
            Assert.Equal(2, list!.Count);
            var seeded = list.Single(q => q.Id == quizId);
            Assert.Equal(1, seeded.QuestionCount);
            Assert.Equal(1, seeded.AttemptCount);
            var empty = list.Single(q => q.Title == "Empty Quiz");
            Assert.Equal(0, empty.QuestionCount);
            Assert.Equal(0, empty.AttemptCount);
        }

        [Fact]
        public async Task A_non_owner_gets_not_found_everywhere_so_existence_never_leaks()
        {
            var (classroomId, quizId) = await SeedQuizAsync();
            var questionId = await FirstQuestionIdAsync(quizId);
            var stranger = Guid.NewGuid();
            var dto = new AddQuestionDto { QuestionType = "TrueFalse", Prompt = "x", Points = 1, CorrectAnswerBool = true };

            Assert.Null(await NewService(NewContext()).GetQuizForEditingAsync(quizId, stranger));
            Assert.Null(await NewService(NewContext()).GetQuizzesForClassroomAsync(classroomId, stranger));
            Assert.Equal(AuthoringOutcome.NotFound, (await NewService(NewContext()).AddQuestionAsync(quizId, stranger, dto)).Outcome);
            Assert.Equal(AuthoringOutcome.NotFound, (await NewService(NewContext()).EditQuestionAsync(quizId, questionId, stranger, dto)).Outcome);
            Assert.Equal(AuthoringOutcome.NotFound, (await NewService(NewContext()).DeleteQuestionAsync(quizId, questionId, stranger)).Outcome);
            // The seeded question is untouched.
            Assert.Single(await QuestionsOfAsync(quizId));
        }

        [Fact]
        public async Task Adding_an_invalid_question_is_rejected_without_persisting()
        {
            var (_, quizId) = await SeedQuizAsync(withQuestion: false);

            var result = await NewService(NewContext()).AddQuestionAsync(quizId, _teacherId, new AddQuestionDto
            {
                QuestionType = "MultipleChoice", Prompt = "Only one option", Points = 1,
                Options = new List<string> { "A" }, CorrectOptionIndex = 0
            });

            Assert.Equal(AuthoringOutcome.Invalid, result.Outcome);
            Assert.NotEmpty(result.Errors);
            Assert.Empty(await QuestionsOfAsync(quizId));
        }

        [Fact]
        public async Task An_unknown_question_type_is_invalid()
        {
            var (_, quizId) = await SeedQuizAsync(withQuestion: false);

            var result = await NewService(NewContext()).AddQuestionAsync(quizId, _teacherId, new AddQuestionDto
            {
                QuestionType = "Essay", Prompt = "Discuss", Points = 1
            });

            Assert.Equal(AuthoringOutcome.Invalid, result.Outcome);
        }

        [Fact]
        public async Task Editing_cannot_change_a_questions_type()
        {
            var (_, quizId) = await SeedQuizAsync(); // a MultipleChoice question
            var questionId = await FirstQuestionIdAsync(quizId);

            var result = await NewService(NewContext()).EditQuestionAsync(quizId, questionId, _teacherId, new AddQuestionDto
            {
                QuestionType = "TrueFalse", Prompt = "Now a bool?", Points = 1, CorrectAnswerBool = true
            });

            Assert.Equal(AuthoringOutcome.Invalid, result.Outcome);
            var stored = await NewContext().Questions.FirstAsync(q => q.Id == questionId);
            Assert.Equal(nameof(MultipleChoiceQuestion), stored.QuestionType);
        }

        [Fact]
        public async Task Editing_or_deleting_a_missing_question_is_not_found()
        {
            var (_, quizId) = await SeedQuizAsync();
            var ghost = Guid.NewGuid();
            var dto = new AddQuestionDto { QuestionType = "TrueFalse", Prompt = "x", Points = 1, CorrectAnswerBool = true };

            Assert.Equal(AuthoringOutcome.NotFound, (await NewService(NewContext()).EditQuestionAsync(quizId, ghost, _teacherId, dto)).Outcome);
            Assert.Equal(AuthoringOutcome.NotFound, (await NewService(NewContext()).DeleteQuestionAsync(quizId, ghost, _teacherId)).Outcome);
        }

        // ---- helpers ----

        /// <summary>A class the seed teacher owns and a quiz in it, optionally with a first
        /// question and an in progress attempt.</summary>
        private async Task<(Guid classroomId, Guid quizId)> SeedQuizAsync(bool withQuestion = true, bool withAttempt = false)
        {
            var classroom = new Classroom(_teacherId, "Authoring Class");
            var quiz = new Quiz(classroom.Id, "Authoring Quiz", 10, _teacherId);
            if (withQuestion)
                quiz.Questions.Add(new MultipleChoiceQuestion("2 + 2?", 5, new List<string> { "3", "4", "5" }, 1));

            await using var ctx = NewContext();
            ctx.Classrooms.Add(classroom);
            ctx.Quizzes.Add(quiz);
            if (withAttempt)
            {
                var attempt = new QuizAttempt(quiz.Id, _studentId);
                attempt.Start(10);
                ctx.QuizAttempts.Add(attempt);
            }
            await ctx.SaveChangesAsync();

            return (classroom.Id, quiz.Id);
        }

        private async Task SeedAttemptAsync(Guid quizId)
        {
            await using var ctx = NewContext();
            var attempt = new QuizAttempt(quizId, _studentId);
            attempt.Start(10);
            ctx.QuizAttempts.Add(attempt);
            await ctx.SaveChangesAsync();
        }

        private async Task<Guid> FirstQuestionIdAsync(Guid quizId) =>
            (await NewContext().Questions.FirstAsync(q => q.QuizId == quizId)).Id;

        private async Task<List<Question>> QuestionsOfAsync(Guid quizId) =>
            await NewContext().Questions.Where(q => q.QuizId == quizId).ToListAsync();

        // A fresh context per call, mirroring the per request scope of the real app.
        private QuizDbContext NewContext() =>
            new(new DbContextOptionsBuilder<QuizDbContext>()
                .UseNpgsql(_postgres.GetConnectionString()).Options);

        private static QuizAppService NewService(QuizDbContext ctx) =>
            new(new QuizRepository(ctx), new QuizAttemptRepository(ctx),
                new GeneratedQuestionDraftRepository(ctx),
                new TemplateQuestionGenerationStrategy(Options.Create(new GenerationOptions())),
                new SourceMaterialExtractor(Options.Create(new GenerationOptions())));
    }
}
