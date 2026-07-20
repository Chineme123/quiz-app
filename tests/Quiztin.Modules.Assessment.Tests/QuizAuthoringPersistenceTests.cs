using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Infrastructure.Persistence;

namespace Quiztin.Modules.Assessment.Tests
{
    /// <summary>
    /// Regression test for the quiz authoring persistence fix, against a real Postgres
    /// (Testcontainers, per code-standards §10, not a substitute provider). Adding a question
    /// to a quiz went through QuizRepository.UpdateAsync, which called DbSet.Update and marked
    /// the whole tracked graph Modified. A freshly added question carries a client generated
    /// Guid key, so EF read it as an existing row and emitted an UPDATE that affected 0 rows,
    /// throwing a phantom concurrency conflict. So AddQuestion (and Generate) never worked over
    /// HTTP against a real database. This is the same bug the attempt side already fixed
    /// (see QuizAttemptPersistenceTests); the in memory provider hid it because it does not
    /// throw on 0 rows affected. Needs Docker (available in CI).
    /// </summary>
    public class QuizAuthoringPersistenceTests : IAsyncLifetime
    {
#pragma warning disable CS0618 // the parameterless builder ctor is deprecated in 4.13; WithImage sets the image
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .Build();
#pragma warning restore CS0618

        private QuizDbContext _context = null!;
        private readonly Guid _teacherId = Guid.NewGuid();
        private Guid _quizId;

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            var options = new DbContextOptionsBuilder<QuizDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;
            _context = new QuizDbContext(options);
            await _context.Database.MigrateAsync();

            // A classroom then a quiz with no questions yet: the state a teacher authors into.
            var classroom = new Classroom(_teacherId, "Authoring Test Classroom");
            var quiz = new Quiz(classroom.Id, "Authoring Test Quiz", 10, _teacherId);
            _context.Classrooms.Add(classroom);
            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
            _quizId = quiz.Id;
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
            await _postgres.DisposeAsync();
        }

        [Fact]
        public async Task AddQuestion_InsertsNewQuestion_WithoutConcurrencyThrow()
        {
            var repository = new QuizRepository(_context);

            // The exact AddQuestionAsync path: load the quiz tracked, add a new question to the
            // collection, then save through UpdateAsync.
            var quiz = await repository.GetByIdAsync(_quizId);
            Assert.NotNull(quiz);
            quiz!.Questions.Add(new MultipleChoiceQuestion("2 + 2?", 1, new List<string> { "3", "4", "5" }, 1)
            {
                QuizId = _quizId
            });

            var thrown = await Record.ExceptionAsync(() => repository.UpdateAsync(quiz));

            // The fix: no phantom concurrency conflict, the new question is INSERTed.
            Assert.Null(thrown);

            using var verifyContext = new QuizDbContext(
                new DbContextOptionsBuilder<QuizDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options);
            var savedQuestions = await verifyContext.Questions
                .Where(q => q.QuizId == _quizId)
                .ToListAsync();
            Assert.Single(savedQuestions);
            Assert.Equal("2 + 2?", savedQuestions[0].Prompt);
        }

        [Fact]
        public async Task AddSecondQuestion_ToQuizThatAlreadyHasOne_InsertsWithoutTouchingTheFirst()
        {
            var repository = new QuizRepository(_context);

            // First question.
            var quiz = await repository.GetByIdAsync(_quizId);
            quiz!.Questions.Add(new TrueFalseQuestion("The sky is blue.", 1, correctAnswer: true) { QuizId = _quizId });
            await repository.UpdateAsync(quiz);

            // Second question, added to a quiz that now has a persisted one: the existing row
            // must not be re-INSERTed and the new one must not be mis-read as an UPDATE.
            var reloaded = await repository.GetByIdAsync(_quizId);
            reloaded!.Questions.Add(new ShortAnswerQuestion("Capital of France?", 1, correctAnswerText: "Paris") { QuizId = _quizId });
            var thrown = await Record.ExceptionAsync(() => repository.UpdateAsync(reloaded));

            Assert.Null(thrown);

            using var verifyContext = new QuizDbContext(
                new DbContextOptionsBuilder<QuizDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options);
            var savedQuestions = await verifyContext.Questions
                .Where(q => q.QuizId == _quizId)
                .ToListAsync();
            Assert.Equal(2, savedQuestions.Count);
        }
    }
}
