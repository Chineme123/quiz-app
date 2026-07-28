using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;
using Quiztin.Modules.Assessment.Application.Services;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Infrastructure.Persistence;
using Quiztin.Modules.Assessment.Infrastructure.Strategies;

namespace Quiztin.Modules.Assessment.Tests
{
    /// <summary>
    /// The student's own results (spec 0011), against a real Postgres because the properties worth
    /// proving are aggregation properties over the attempt tables: grouping by class, the latest
    /// submitted per quiz, the per class standing (percentage normalized), the archived class
    /// staying, and the scoping to the caller's own attempts. These are the rules a green unit test
    /// on a mock would miss. Needs Docker.
    /// </summary>
    public class MyResultsTests : IAsyncLifetime
    {
#pragma warning disable CS0618 // the parameterless builder ctor is deprecated in 4.13; WithImage sets the image
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .Build();
#pragma warning restore CS0618

        private QuizDbContext _context = null!;
        private readonly Guid _teacherId = Guid.NewGuid();
        private readonly Guid _sam = Guid.NewGuid();
        private readonly Guid _alex = Guid.NewGuid();

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
        public async Task Groups_finished_quizzes_by_class_with_the_latest_submitted_and_a_standing()
        {
            // Maths has two quizzes; Science one big quiz. Different sizes, so a raw point average
            // would be meaningless — the standing normalizes each to a percentage first (AC-6).
            var maths = new Classroom(_teacherId, "Maths");
            var quizA = new Quiz(maths.Id, "Quiz A", 10, _teacherId) { IsPublished = true };
            var aQs = AddQuestions(quizA, 5, 5);        // total 10
            var quizB = new Quiz(maths.Id, "Quiz B", 10, _teacherId) { IsPublished = true };
            var bQs = AddQuestions(quizB, 5, 5);        // total 10
            var science = new Classroom(_teacherId, "Science");
            var big = new Quiz(science.Id, "Big", 10, _teacherId) { IsPublished = true };
            var bigQs = AddQuestions(big, 80, 20);      // total 100

            await using (var ctx = NewContext())
            {
                ctx.Classrooms.AddRange(maths, science);
                ctx.Quizzes.AddRange(quizA, quizB, big);
                ctx.Enrollments.Add(new Enrollment(_sam, maths.Id));
                ctx.Enrollments.Add(new Enrollment(_sam, science.Id));
                await ctx.SaveChangesAsync();
            }

            // Quiz A twice: an early 0/10, then a later 5/10 — only the latest counts (AC-4).
            await SeedSubmittedAsync(quizA, aQs, _sam, right: 0);
            await Task.Delay(15);
            await SeedSubmittedAsync(quizA, aQs, _sam, right: 1);   // latest 5/10 -> 50%
            await SeedSubmittedAsync(quizB, bQs, _sam, right: 2);   // 10/10 -> 100%
            await SeedSubmittedAsync(big, bigQs, _sam, right: 1);   // 80/100 -> 80%

            await using var read = NewContext();
            var results = await Service(read).GetMyResultsAsync(_sam);

            Assert.Equal(2, results.Classrooms.Count);
            var mathsGroup = results.Classrooms.Single(c => c.ClassroomName == "Maths");
            var scienceGroup = results.Classrooms.Single(c => c.ClassroomName == "Science");

            // Maths: two rows, each the latest submitted; the early 0 is gone, not a third row.
            Assert.Equal(2, mathsGroup.Quizzes.Count);
            var rowA = mathsGroup.Quizzes.Single(q => q.QuizId == quizA.Id);
            var rowB = mathsGroup.Quizzes.Single(q => q.QuizId == quizB.Id);
            Assert.Equal(5m, rowA.Score);
            Assert.Equal(50m, rowA.Percent);
            Assert.Equal(10m, rowB.Score);
            Assert.Equal(100m, rowB.Percent);
            Assert.Equal(75m, mathsGroup.StandingPercent);   // (50 + 100) / 2, over one attempt each

            // Science: one row, standing is its single percentage.
            var rowBig = Assert.Single(scienceGroup.Quizzes);
            Assert.Equal(80m, rowBig.Score);
            Assert.Equal(80m, rowBig.Percent);
            Assert.Equal(100, rowBig.TotalPoints);
            Assert.Equal(80m, scienceGroup.StandingPercent);
        }

        [Fact]
        public async Task Returns_only_the_callers_own_finished_quizzes()
        {
            var shared = new Classroom(_teacherId, "Shared");
            var quiz = new Quiz(shared.Id, "Q", 10, _teacherId) { IsPublished = true };
            var qs = AddQuestions(quiz, 5, 5);

            await using (var ctx = NewContext())
            {
                ctx.Classrooms.Add(shared);
                ctx.Quizzes.Add(quiz);
                ctx.Enrollments.Add(new Enrollment(_sam, shared.Id));
                ctx.Enrollments.Add(new Enrollment(_alex, shared.Id));
                await ctx.SaveChangesAsync();
            }

            await SeedSubmittedAsync(quiz, qs, _sam, right: 2);    // Sam 10/10
            await SeedSubmittedAsync(quiz, qs, _alex, right: 1);   // Alex 5/10

            await using var read = NewContext();

            // Sam sees his own score on the shared quiz, never Alex's.
            var samRow = Assert.Single(Assert.Single((await Service(read).GetMyResultsAsync(_sam)).Classrooms).Quizzes);
            Assert.Equal(10m, samRow.Score);

            // Alex, querying with their own id, sees their own score. No id is ever passed in the
            // request, so one student cannot ask for another's results (AC-1).
            var alexRow = Assert.Single(Assert.Single((await Service(read).GetMyResultsAsync(_alex)).Classrooms).Quizzes);
            Assert.Equal(5m, alexRow.Score);
        }

        [Fact]
        public async Task Only_submitted_quizzes_appear_in_progress_and_untaken_are_excluded()
        {
            var classroom = new Classroom(_teacherId, "Class");
            var finished = new Quiz(classroom.Id, "Finished", 10, _teacherId) { IsPublished = true };
            var fQs = AddQuestions(finished, 5, 5);
            var started = new Quiz(classroom.Id, "Started", 10, _teacherId) { IsPublished = true };
            AddQuestions(started, 5, 5);
            var untaken = new Quiz(classroom.Id, "Untaken", 10, _teacherId) { IsPublished = true };
            AddQuestions(untaken, 5, 5);

            await using (var ctx = NewContext())
            {
                ctx.Classrooms.Add(classroom);
                ctx.Quizzes.AddRange(finished, started, untaken);
                ctx.Enrollments.Add(new Enrollment(_sam, classroom.Id));
                await ctx.SaveChangesAsync();
            }

            await SeedSubmittedAsync(finished, fQs, _sam, right: 2);   // finished
            await StartOpenAttemptAsync(started, _sam);               // in progress, not submitted
            // untaken: never started.

            await using var read = NewContext();
            var group = Assert.Single((await Service(read).GetMyResultsAsync(_sam)).Classrooms);

            var row = Assert.Single(group.Quizzes);   // only the finished quiz is a row (AC-3)
            Assert.Equal(finished.Id, row.QuizId);
            Assert.DoesNotContain(group.Quizzes, q => q.QuizId == started.Id);
            Assert.DoesNotContain(group.Quizzes, q => q.QuizId == untaken.Id);
        }

        [Fact]
        public async Task An_archived_class_the_student_is_in_still_shows_its_results()
        {
            var classroom = new Classroom(_teacherId, "Archived class");
            var quiz = new Quiz(classroom.Id, "Q", 10, _teacherId) { IsPublished = true };
            var qs = AddQuestions(quiz, 5, 5);

            await using (var ctx = NewContext())
            {
                ctx.Classrooms.Add(classroom);
                ctx.Quizzes.Add(quiz);
                ctx.Enrollments.Add(new Enrollment(_sam, classroom.Id));
                await ctx.SaveChangesAsync();
            }

            await SeedSubmittedAsync(quiz, qs, _sam, right: 2);

            await using (var ctx = NewContext())
            {
                var c = await ctx.Classrooms.FirstAsync(x => x.Id == classroom.Id);
                c.ArchivedAt = DateTime.UtcNow;
                await ctx.SaveChangesAsync();
            }

            await using var read = NewContext();
            var group = Assert.Single((await Service(read).GetMyResultsAsync(_sam)).Classrooms);

            Assert.True(group.IsArchived);
            Assert.Single(group.Quizzes);   // archived, but still the student's own history (AC-7)
        }

        [Fact]
        public async Task A_student_with_no_enrolments_gets_an_empty_result_not_an_error()
        {
            await using var read = NewContext();
            var results = await Service(read).GetMyResultsAsync(Guid.NewGuid());

            Assert.Empty(results.Classrooms);   // a normal empty payload, never null or an error (AC-8)
        }

        [Fact]
        public async Task An_enrolled_class_with_nothing_finished_appears_as_an_empty_group()
        {
            var classroom = new Classroom(_teacherId, "Empty");
            var quiz = new Quiz(classroom.Id, "Q", 10, _teacherId) { IsPublished = true };
            AddQuestions(quiz, 5, 5);

            await using (var ctx = NewContext())
            {
                ctx.Classrooms.Add(classroom);
                ctx.Quizzes.Add(quiz);
                ctx.Enrollments.Add(new Enrollment(_sam, classroom.Id));
                await ctx.SaveChangesAsync();
            }

            await using var read = NewContext();
            var group = Assert.Single((await Service(read).GetMyResultsAsync(_sam)).Classrooms);

            Assert.Empty(group.Quizzes);          // the class appears, not omitted (AC-2)
            Assert.Null(group.StandingPercent);   // with no standing, since nothing is finished (AC-6)
        }

        // ---- fixtures & helpers -------------------------------------------------------------

        /// <summary>Two multiple-choice questions (correct answer at index 0) added to a quiz.</summary>
        private static IReadOnlyList<Question> AddQuestions(Quiz quiz, int firstPoints, int secondPoints)
        {
            var q1 = new MultipleChoiceQuestion("Q1", firstPoints, new List<string> { "right", "wrong" }, 0);
            var q2 = new MultipleChoiceQuestion("Q2", secondPoints, new List<string> { "right", "wrong" }, 0);
            quiz.Questions.Add(q1);
            quiz.Questions.Add(q2);
            return new List<Question> { q1, q2 };
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

        private static MyResultsAppService Service(QuizDbContext ctx) =>
            new(new ClassroomRepository(ctx),
                new QuizRepository(ctx),
                new ResultsReadRepository(ctx));
    }
}
