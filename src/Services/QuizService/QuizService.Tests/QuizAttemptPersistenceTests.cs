using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;
using QuizService.Domain.Entities;
using QuizService.Domain.Enums;
using QuizService.Infrastructure.Persistence;
using QuizService.Infrastructure.Strategies;

namespace QuizService.Tests
{
    /// <summary>
    /// Regression tests for the two submit persistence fixes (spec 0005), against a real
    /// Postgres (Testcontainers, per code-standards §10, not a substitute provider).
    /// Submit adds new answers to a tracked attempt; before the fixes EF marked them as
    /// existing rows and the save threw a phantom concurrency conflict, so submit never
    /// worked end to end. These lock that it INSERTs the answers and grades cleanly.
    /// Needs Docker (available in CI).
    /// </summary>
    public class QuizAttemptPersistenceTests : IAsyncLifetime
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

            // The FK chain an attempt needs: a classroom then a published quiz.
            var classroom = new Classroom(_teacherId, "Persistence Test Classroom");
            var quiz = new Quiz(classroom.Id, "Persistence Test Quiz", 10, _teacherId) { IsPublished = true };
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
        public async Task Submit_InsertsNewAnswers_AndGrades_WithoutConcurrencyThrow()
        {
            var repository = new QuizAttemptRepository(_context);
            var studentId = Guid.NewGuid();

            // Start an attempt and persist it (this is what StartQuiz does).
            var attempt = new QuizAttempt(_quizId, studentId);
            attempt.Start();
            await repository.AddAsync(attempt);

            // Reload it tracked, submit answers, grade, and save — the exact submit path.
            var question = new MultipleChoiceQuestion("2 + 2?", 10, new List<string> { "3", "4", "5" }, 1);
            var loaded = await repository.GetByIdAsync(attempt.Id);
            loaded.Submit(new List<QuizAnswer> { new QuizAnswer(question.Id, "1") }); // correct
            loaded.Evaluate(new PointsScoringStrategy(), new List<Question> { question });

            var thrown = await Record.ExceptionAsync(() => repository.UpdateAsync(loaded));

            // The fix: no phantom concurrency conflict, the new answer is INSERTed.
            Assert.Null(thrown);

            using var verifyContext = new QuizDbContext(
                new DbContextOptionsBuilder<QuizDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options);
            var savedAnswers = await verifyContext.QuizAnswers
                .Where(a => a.QuizAttemptId == attempt.Id)
                .ToListAsync();
            Assert.Single(savedAnswers);
            Assert.True(savedAnswers[0].IsCorrect);
            Assert.Equal(10m, savedAnswers[0].PointsAwarded);

            var savedAttempt = await verifyContext.QuizAttempts.FirstAsync(a => a.Id == attempt.Id);
            Assert.Equal("Graded", savedAttempt.CurrentStateName);
            Assert.Equal(10m, savedAttempt.TotalScore);
            Assert.Equal(FeedbackStatus.Pending, savedAttempt.FeedbackStatus);
        }

        [Fact]
        public async Task BackgroundFeedbackSave_UpdatesExistingAnswers_AndMarksReady()
        {
            var repository = new QuizAttemptRepository(_context);
            var studentId = Guid.NewGuid();
            var question = new MultipleChoiceQuestion("2 + 2?", 10, new List<string> { "3", "4", "5" }, 1);

            var attempt = new QuizAttempt(_quizId, studentId);
            attempt.Start();
            await repository.AddAsync(attempt);
            var graded = await repository.GetByIdAsync(attempt.Id);
            graded.Submit(new List<QuizAnswer> { new QuizAnswer(question.Id, "0") }); // incorrect
            graded.Evaluate(new PointsScoringStrategy(), new List<Question> { question });
            await repository.UpdateAsync(graded);

            // The background worker path: reload, write feedback on the EXISTING answers,
            // mark ready, save. This must UPDATE the answers, not fail.
            var forFeedback = await repository.GetByIdAsync(attempt.Id);
            await new StandardFeedbackStrategy().GenerateAsync(forFeedback, new List<Question> { question });
            forFeedback.MarkFeedbackReady();
            var thrown = await Record.ExceptionAsync(() => repository.UpdateAsync(forFeedback));

            Assert.Null(thrown);
            using var verifyContext = new QuizDbContext(
                new DbContextOptionsBuilder<QuizDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options);
            var savedAttempt = await verifyContext.QuizAttempts.FirstAsync(a => a.Id == attempt.Id);
            Assert.Equal("Reviewable", savedAttempt.CurrentStateName);
            Assert.Equal(FeedbackStatus.Ready, savedAttempt.FeedbackStatus);
            var savedAnswer = await verifyContext.QuizAnswers.FirstAsync(a => a.QuizAttemptId == attempt.Id);
            Assert.False(string.IsNullOrWhiteSpace(savedAnswer.Feedback));
            Assert.Equal(FeedbackSource.Deterministic, savedAnswer.FeedbackSource);
        }
    }
}
