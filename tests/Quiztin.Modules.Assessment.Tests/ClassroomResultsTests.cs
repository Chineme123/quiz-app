using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;
using Quiztin.Modules.Assessment.Application.Facades;
using Quiztin.Modules.Assessment.Application.Invokers;
using Quiztin.Modules.Assessment.Application.Services;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Events;
using Quiztin.Modules.Assessment.Infrastructure.Factories;
using Quiztin.Modules.Assessment.Infrastructure.Persistence;
using Quiztin.Modules.Assessment.Infrastructure.Strategies;
using Quiztin.Modules.Identity.Contracts;

namespace Quiztin.Modules.Assessment.Tests
{
    /// <summary>
    /// Teacher classroom results (spec 0010), against a real Postgres because the properties worth
    /// proving here are aggregation properties over the attempt tables: the dedup to one attempt
    /// per student, the "latest submitted" rule, and the percentage normalization the design's
    /// cross-check flagged. These are the rules a green unit test on a mock would miss. Needs Docker.
    /// </summary>
    public class ClassroomResultsTests : IAsyncLifetime
    {
#pragma warning disable CS0618 // the parameterless builder ctor is deprecated in 4.13; WithImage sets the image
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .Build();
#pragma warning restore CS0618

        private QuizDbContext _context = null!;
        private readonly Guid _teacherId = Guid.NewGuid();
        private readonly Guid _alice = Guid.NewGuid();
        private readonly Guid _bob = Guid.NewGuid();
        private readonly Guid _carol = Guid.NewGuid();
        private readonly Guid _dave = Guid.NewGuid();

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
        public async Task Summary_averages_the_latest_submitted_per_student_and_counts_each_once()
        {
            var (classroomId, quiz, questions) = await SeedClassroomWithQuizAsync("Quiz One", 5, 5);
            await EnrolAsync(classroomId, _alice, _bob);

            // alice: one attempt, 5/10 (first question right, second wrong).
            await SeedSubmittedAsync(quiz, questions, _alice, right: 1);
            // bob: an early 5/10, then a later retry at 10/10 — only the retry may count (AC-7).
            await SeedSubmittedAsync(quiz, questions, _bob, right: 1);
            await Task.Delay(15);
            await SeedSubmittedAsync(quiz, questions, _bob, right: 2);

            await using var read = NewContext();
            var summary = await ResultsService(read).GetClassroomSummaryAsync(classroomId, _teacherId);

            Assert.NotNull(summary);
            Assert.Equal(2, summary.StudentCount);
            var row = Assert.Single(summary.Quizzes);
            Assert.Equal(10, row.TotalPoints);
            Assert.Equal(2, row.CompletionCount);   // alice + bob, each once (bob's early try excluded)
            Assert.Equal(7.5m, row.AverageScore);    // (5 + 10) / 2, not (5 + 5 + 10) / 3
            Assert.Equal(75m, row.AveragePercent);
        }

        [Fact]
        public async Task PerQuiz_shows_status_and_latest_score_by_name_and_counts_questions_once()
        {
            var (classroomId, quiz, questions) = await SeedClassroomWithQuizAsync("Quiz One", 5, 5);
            await EnrolAsync(classroomId, _alice, _bob, _carol, _dave);

            await SeedSubmittedAsync(quiz, questions, _alice, right: 1);   // 5/10
            await SeedSubmittedAsync(quiz, questions, _bob, right: 1);     // early 5/10
            await Task.Delay(15);
            await SeedSubmittedAsync(quiz, questions, _bob, right: 2);     // latest 10/10
            await StartOpenAttemptAsync(quiz, _carol);                    // in progress
            // dave: enrolled, never attempted.

            var directory = new StubDirectory(
                new UserIdentity(_alice, "Alice", "alice@test.edu"),
                new UserIdentity(_bob, "Bob", "bob@test.edu"),
                new UserIdentity(_carol, "Carol", "carol@test.edu"),
                new UserIdentity(_dave, null, "dave@test.edu"));          // no display name -> email

            await using var read = NewContext();
            var result = await ResultsService(read, directory).GetQuizResultsAsync(quiz.Id, _teacherId, 1, 20);

            Assert.NotNull(result);
            Assert.Equal(4, result.Total);
            Assert.Equal(2, result.CompletionCount);
            Assert.Equal(7.5m, result.AverageScore);

            var byStudent = result.Students.ToDictionary(s => s.StudentId);
            Assert.Equal("Alice", byStudent[_alice].DisplayName);
            Assert.Equal("Completed", byStudent[_alice].Status);
            Assert.Equal(5m, byStudent[_alice].Score);

            Assert.Equal("Bob", byStudent[_bob].DisplayName);
            Assert.Equal("Completed", byStudent[_bob].Status);
            Assert.Equal(10m, byStudent[_bob].Score);      // the latest submitted, not the early 5
            Assert.NotNull(byStudent[_bob].AttemptId);

            Assert.Equal("InProgress", byStudent[_carol].Status);
            Assert.Null(byStudent[_carol].Score);

            Assert.Equal("NotTaken", byStudent[_dave].Status);
            Assert.Equal("dave@test.edu", byStudent[_dave].DisplayName);  // email fallback (AC-13)

            // Per-question fraction is over one attempt per student (alice + bob's latest): the
            // first question is right for both (2/2), the second right only for bob's latest (1/2).
            // Bob's early wrong second answer must not drag it to 1/3.
            var q1 = result.Questions.Single(q => q.QuestionId == questions[0].Id);
            var q2 = result.Questions.Single(q => q.QuestionId == questions[1].Id);
            Assert.Equal(2, q1.AnsweredCount);
            Assert.Equal(100m, q1.FractionCorrect);
            Assert.Equal(50m, q2.FractionCorrect);
        }

        [Fact]
        public async Task PerQuiz_a_submitted_score_stands_over_a_newer_open_attempt()
        {
            var (classroomId, quiz, questions) = await SeedClassroomWithQuizAsync("Quiz One", 5, 5);
            await EnrolAsync(classroomId, _alice);

            await SeedSubmittedAsync(quiz, questions, _alice, right: 2);   // 10/10 submitted
            await Task.Delay(15);
            await StartOpenAttemptAsync(quiz, _alice);                    // a newer, still-open attempt

            await using var read = NewContext();
            var result = await ResultsService(read, new StubDirectory(new UserIdentity(_alice, "Alice", "a@test.edu")))
                .GetQuizResultsAsync(quiz.Id, _teacherId, 1, 20);

            var alice = Assert.Single(result!.Students);
            Assert.Equal("Completed", alice.Status);   // the submitted result stands (AC-8), not InProgress
            Assert.Equal(10m, alice.Score);
            Assert.Equal(1, result.CompletionCount);
        }

        [Fact]
        public async Task Rollup_normalizes_each_quiz_to_a_percentage_for_the_overall_standing()
        {
            // Two differently sized quizzes: raw point totals would be meaningless to average.
            var classroom = new Classroom(_teacherId, "Mixed sizes");
            var small = new Quiz(classroom.Id, "Small", 10, _teacherId) { IsPublished = true };
            var smallQs = AddQuestions(small, 5, 5);        // total 10
            var big = new Quiz(classroom.Id, "Big", 10, _teacherId) { IsPublished = true };
            var bigQs = AddQuestions(big, 80, 20);          // total 100

            await using (var ctx = NewContext())
            {
                ctx.Classrooms.Add(classroom);
                ctx.Quizzes.Add(small);
                ctx.Quizzes.Add(big);
                ctx.Enrollments.Add(new Enrollment(_alice, classroom.Id));
                await ctx.SaveChangesAsync();
            }

            await SeedSubmittedAsync(small, smallQs, _alice, right: 1);   // 5/10  -> 50%
            await SeedSubmittedAsync(big, bigQs, _alice, right: 1);       // 80/100 -> 80%

            await using var read = NewContext();
            var rollup = await ResultsService(read, new StubDirectory(new UserIdentity(_alice, "Alice", "a@test.edu")))
                .GetStudentRollupAsync(classroom.Id, _teacherId, 1, 20);

            Assert.NotNull(rollup);
            Assert.Equal(2, rollup.Quizzes.Count);
            var alice = Assert.Single(rollup.Students);
            Assert.Equal(65m, alice.OverallStandingPercent);   // (50 + 80) / 2, not an average of raw points
            var smallCell = alice.Scores.Single(s => s.QuizId == small.Id);
            var bigCell = alice.Scores.Single(s => s.QuizId == big.Id);
            Assert.Equal(50m, smallCell.Percent);
            Assert.Equal(80m, bigCell.Percent);
        }

        [Fact]
        public async Task Results_are_served_for_an_archived_classroom_to_its_owner()
        {
            var (classroomId, quiz, questions) = await SeedClassroomWithQuizAsync("Quiz One", 5, 5);
            await EnrolAsync(classroomId, _alice);
            await SeedSubmittedAsync(quiz, questions, _alice, right: 2);

            await using (var ctx = NewContext())
            {
                var classroom = await ctx.Classrooms.FirstAsync(c => c.Id == classroomId);
                classroom.ArchivedAt = DateTime.UtcNow;
                await ctx.SaveChangesAsync();
            }

            await using var read = NewContext();
            var summary = await ResultsService(read).GetClassroomSummaryAsync(classroomId, _teacherId);

            Assert.NotNull(summary);
            Assert.True(summary.IsArchived);
            Assert.Single(summary.Quizzes);   // still served (AC-9)
        }

        [Fact]
        public async Task Results_are_owner_scoped_a_non_owner_and_a_missing_id_both_return_null()
        {
            var (classroomId, quiz, _) = await SeedClassroomWithQuizAsync("Quiz One", 5, 5);
            var stranger = Guid.NewGuid();

            await using var read = NewContext();
            var service = ResultsService(read);

            // A non-owner (AC-1).
            Assert.Null(await service.GetClassroomSummaryAsync(classroomId, stranger));
            Assert.Null(await service.GetQuizResultsAsync(quiz.Id, stranger, 1, 20));
            Assert.Null(await service.GetStudentRollupAsync(classroomId, stranger, 1, 20));

            // A missing id, indistinguishable from a non-owned one.
            Assert.Null(await service.GetClassroomSummaryAsync(Guid.NewGuid(), _teacherId));
            Assert.Null(await service.GetQuizResultsAsync(Guid.NewGuid(), _teacherId, 1, 20));
            Assert.Null(await service.GetStudentRollupAsync(Guid.NewGuid(), _teacherId, 1, 20));
        }

        [Fact]
        public async Task Drilldown_returns_the_latest_submitted_attempt_and_is_owner_scoped()
        {
            var (classroomId, quiz, questions) = await SeedClassroomWithQuizAsync("Quiz One", 5, 5);
            await EnrolAsync(classroomId, _bob, _dave);

            await SeedSubmittedAsync(quiz, questions, _bob, right: 1);   // early 5/10
            await Task.Delay(15);
            await SeedSubmittedAsync(quiz, questions, _bob, right: 2);   // latest 10/10

            await using var read = NewContext();
            var facade = BuildFacade(read);

            var result = await facade.GetOwnedStudentResultAsync(quiz.Id, _bob, _teacherId);
            Assert.NotNull(result);
            Assert.Equal(10m, result.TotalScore);          // the latest submitted (AC-6, AC-7)
            Assert.Equal(2, result.Answers.Count);

            // A non-owner sees nothing, and a student with no submitted attempt is a 404 too.
            Assert.Null(await facade.GetOwnedStudentResultAsync(quiz.Id, _bob, Guid.NewGuid()));
            Assert.Null(await facade.GetOwnedStudentResultAsync(quiz.Id, _dave, _teacherId));
        }

        // ---- fixtures & helpers -------------------------------------------------------------

        /// <summary>A published quiz whose two questions carry the given points, in a new classroom
        /// owned by the seed teacher. Returns the classroom id, the quiz, and its questions.</summary>
        private async Task<(Guid ClassroomId, Quiz Quiz, IReadOnlyList<Question> Questions)>
            SeedClassroomWithQuizAsync(string title, int firstPoints, int secondPoints)
        {
            var classroom = new Classroom(_teacherId, "Class");
            var quiz = new Quiz(classroom.Id, title, 10, _teacherId) { IsPublished = true };
            var questions = AddQuestions(quiz, firstPoints, secondPoints);

            await using var ctx = NewContext();
            ctx.Classrooms.Add(classroom);
            ctx.Quizzes.Add(quiz);
            await ctx.SaveChangesAsync();

            return (classroom.Id, quiz, questions);
        }

        /// <summary>Two multiple-choice questions (correct answer at index 0) added to a quiz.</summary>
        private static IReadOnlyList<Question> AddQuestions(Quiz quiz, int firstPoints, int secondPoints)
        {
            var q1 = new MultipleChoiceQuestion("Q1", firstPoints, new List<string> { "right", "wrong" }, 0);
            var q2 = new MultipleChoiceQuestion("Q2", secondPoints, new List<string> { "right", "wrong" }, 0);
            quiz.Questions.Add(q1);
            quiz.Questions.Add(q2);
            return new List<Question> { q1, q2 };
        }

        private async Task EnrolAsync(Guid classroomId, params Guid[] studentIds)
        {
            await using var ctx = NewContext();
            foreach (var id in studentIds) ctx.Enrollments.Add(new Enrollment(id, classroomId));
            await ctx.SaveChangesAsync();
        }

        /// <summary>Seeds one submitted, graded attempt. <paramref name="right"/> is how many of the
        /// two questions the student answered correctly (0, 1, or 2), so the score is deterministic.</summary>
        private async Task SeedSubmittedAsync(Quiz quiz, IReadOnlyList<Question> questions, Guid studentId, int right)
        {
            var answers = new Dictionary<Guid, string>
            {
                [questions[0].Id] = right >= 1 ? "0" : "1",   // "0" is correct
                [questions[1].Id] = right >= 2 ? "0" : "1"
            };

            await using var ctx = NewContext();
            var attempt = new QuizAttempt(quiz.Id, studentId);
            attempt.Start(10);
            attempt.SaveDraftAnswers(answers, DateTime.UtcNow);
            attempt.Submit();
            attempt.Evaluate(new PointsScoringStrategy(), questions);
            ctx.QuizAttempts.Add(attempt);
            await ctx.SaveChangesAsync();
        }

        private async Task StartOpenAttemptAsync(Quiz quiz, Guid studentId)
        {
            await using var ctx = NewContext();
            var attempt = new QuizAttempt(quiz.Id, studentId);
            attempt.Start(10);
            ctx.QuizAttempts.Add(attempt);
            await ctx.SaveChangesAsync();
        }

        private QuizDbContext NewContext() =>
            new(new DbContextOptionsBuilder<QuizDbContext>()
                .UseNpgsql(_postgres.GetConnectionString()).Options);

        private ClassroomResultsAppService ResultsService(QuizDbContext ctx, IUserDirectory? directory = null) =>
            new(new ClassroomRepository(ctx),
                new QuizRepository(ctx),
                new ResultsReadRepository(ctx),
                directory ?? new StubDirectory());

        private static TakeQuizFacade BuildFacade(QuizDbContext ctx) =>
            new(new QuizRepository(ctx),
                new QuizAttemptRepository(ctx),
                new StrategyFactory(),
                new QuizCommandInvoker(),
                new NoOpEventDispatcher());

        /// <summary>A stand-in for the Identity user directory: the aggregation is what these tests
        /// prove, so names are supplied directly rather than crossing the module boundary.</summary>
        private sealed class StubDirectory : IUserDirectory
        {
            private readonly IReadOnlyDictionary<Guid, UserIdentity> _users;

            public StubDirectory(params UserIdentity[] users) =>
                _users = users.ToDictionary(u => u.UserId);

            public Task<IReadOnlyDictionary<Guid, UserIdentity>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds)
            {
                IReadOnlyDictionary<Guid, UserIdentity> result = userIds
                    .Distinct()
                    .Where(_users.ContainsKey)
                    .ToDictionary(id => id, id => _users[id]);
                return Task.FromResult(result);
            }
        }

        private sealed class NoOpEventDispatcher : IEventDispatcher
        {
            public Task DispatchAsync<TEvent>(TEvent domainEvent) => Task.CompletedTask;
        }
    }
}
