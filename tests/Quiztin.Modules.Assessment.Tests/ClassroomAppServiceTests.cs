using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;
using Quiztin.Modules.Assessment.Application.Facades;
using Quiztin.Modules.Assessment.Application.Invokers;
using Quiztin.Modules.Assessment.Application.Results;
using Quiztin.Modules.Assessment.Application.Services;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Events;
using Quiztin.Modules.Assessment.Infrastructure.Factories;
using Quiztin.Modules.Assessment.Infrastructure.Persistence;

namespace Quiztin.Modules.Assessment.Tests
{
    /// <summary>
    /// Classroom create, join, and management (spec 0008), against a real Postgres because the
    /// properties worth proving here are database properties: the unique index is what makes
    /// join idempotent under a race, and an in memory stand in would not enforce it.
    /// Needs Docker (available in CI).
    /// </summary>
    public class ClassroomAppServiceTests : IAsyncLifetime
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
        public async Task Create_issues_a_join_code_and_scopes_the_class_to_its_teacher()
        {
            var result = await NewService(_context).CreateAsync(_teacherId, "  Biology 101  ");

            Assert.Equal(ClassroomOutcome.Ok, result.Outcome);
            Assert.NotNull(result.Classroom);
            Assert.Equal("Biology 101", result.Classroom!.Name); // trimmed
            Assert.Equal(6, result.Classroom.JoinCode.Length);
            Assert.Null(result.Classroom.ArchivedAt);

            var stored = await NewContext().Classrooms.SingleAsync(c => c.Id == result.Classroom.Id);
            Assert.Equal(_teacherId, stored.TeacherId);
        }

        [Fact]
        public async Task Create_rejects_a_blank_or_overlong_name()
        {
            var service = NewService(_context);

            Assert.Equal(ClassroomOutcome.InvalidName, (await service.CreateAsync(_teacherId, "   ")).Outcome);
            Assert.Equal(ClassroomOutcome.InvalidName,
                (await service.CreateAsync(_teacherId, new string('x', 101))).Outcome);
        }

        [Fact]
        public async Task Joining_by_code_enrols_the_student()
        {
            var code = await SeedClassroomAsync();

            var result = await NewService(NewContext()).JoinAsync(code, _studentId);

            Assert.Equal(JoinOutcome.Joined, result.Outcome);
            Assert.True(await NewContext().Enrollments
                .AnyAsync(e => e.StudentId == _studentId && e.ClassroomId == result.ClassroomId));
        }

        [Fact]
        public async Task Joining_is_case_insensitive_and_idempotent()
        {
            var code = await SeedClassroomAsync();

            var first = await NewService(NewContext()).JoinAsync(code, _studentId);
            var second = await NewService(NewContext()).JoinAsync(code.ToLowerInvariant(), _studentId);

            Assert.Equal(JoinOutcome.Joined, first.Outcome);
            // Already in the class is still a success, and it never produces a second row.
            Assert.Equal(JoinOutcome.AlreadyEnrolled, second.Outcome);
            Assert.Equal(1, await NewContext().Enrollments.CountAsync(e => e.StudentId == _studentId));
        }

        [Fact]
        public async Task Two_concurrent_joins_produce_exactly_one_enrolment()
        {
            var code = await SeedClassroomAsync();

            // Separate contexts, mirroring two simultaneous requests. Neither reads before it
            // writes, so the unique index is the only arbiter: one inserts, the other catches the
            // constraint and reports already enrolled rather than failing (AC-3).
            var both = await Task.WhenAll(
                NewService(NewContext()).JoinAsync(code, _studentId),
                NewService(NewContext()).JoinAsync(code, _studentId));

            Assert.All(both, r => Assert.Contains(r.Outcome, new[] { JoinOutcome.Joined, JoinOutcome.AlreadyEnrolled }));
            Assert.Equal(1, await NewContext().Enrollments.CountAsync(e => e.StudentId == _studentId));
        }

        [Fact]
        public async Task Joining_a_class_you_own_is_refused()
        {
            var code = await SeedClassroomAsync();

            var result = await NewService(NewContext()).JoinAsync(code, _teacherId);

            Assert.Equal(JoinOutcome.OwnClassroom, result.Outcome);
            Assert.False(await NewContext().Enrollments.AnyAsync(e => e.StudentId == _teacherId));
        }

        [Fact]
        public async Task An_archived_class_stops_resolving_by_code()
        {
            var code = await SeedClassroomAsync();
            var classroomId = (await NewContext().Classrooms.SingleAsync()).Id;
            await NewService(NewContext()).ArchiveAsync(classroomId, _teacherId);

            Assert.Equal(JoinOutcome.NotFound, (await NewService(NewContext()).JoinAsync(code, _studentId)).Outcome);
            Assert.Null(await NewService(NewContext()).PreviewByCodeAsync(code, _studentId));
        }

        [Fact]
        public async Task Archiving_hides_the_quiz_from_the_available_list_and_blocks_starting_it()
        {
            var (classroomId, quizId) = await SeedClassroomWithPublishedQuizAsync();

            // Before archiving, the enrolled student can see and start it.
            var (before, _) = await new QuizRepository(NewContext()).GetAvailableForStudentAsync(_studentId, 0, 20);
            Assert.Single(before);

            await NewService(NewContext()).ArchiveAsync(classroomId, _teacherId);

            // AC-8: archiving must stop the quiz listing AND stop it starting. Enforced on the
            // take side, which is the half that is easy to leave unwired.
            var (after, total) = await new QuizRepository(NewContext()).GetAvailableForStudentAsync(_studentId, 0, 20);
            Assert.Empty(after);
            Assert.Equal(0, total);

            await Assert.ThrowsAsync<Exception>(() =>
                BuildFacade(NewContext()).StartQuizAsync(_studentId, quizId));

            // Unarchiving puts it back, so nothing was destroyed.
            await NewService(NewContext()).UnarchiveAsync(classroomId, _teacherId);
            var (restored, _) = await new QuizRepository(NewContext()).GetAvailableForStudentAsync(_studentId, 0, 20);
            Assert.Single(restored);
        }

        [Fact]
        public async Task A_non_owner_managing_a_class_gets_not_found_so_existence_never_leaks()
        {
            await SeedClassroomAsync();
            var classroomId = (await NewContext().Classrooms.SingleAsync()).Id;
            var stranger = Guid.NewGuid();
            var service = NewService(NewContext());

            Assert.Equal(ClassroomOutcome.NotFound, await service.RenameAsync(classroomId, stranger, "Hijacked"));
            Assert.Equal(ClassroomOutcome.NotFound, await service.ArchiveAsync(classroomId, stranger));
            Assert.Equal(ClassroomOutcome.NotFound, await service.UnarchiveAsync(classroomId, stranger));
            Assert.Equal(ClassroomOutcome.NotFound, (await service.RegenerateJoinCodeAsync(classroomId, stranger)).Outcome);
            Assert.Equal(ClassroomOutcome.NotFound, await service.RemoveStudentAsync(classroomId, stranger, _studentId));
            Assert.Null(await service.GetRosterAsync(classroomId, stranger, 1, 20));

            // A class that does not exist reads exactly the same, which is the point.
            Assert.Equal(ClassroomOutcome.NotFound, await service.RenameAsync(Guid.NewGuid(), _teacherId, "Ghost"));
        }

        [Fact]
        public async Task Regenerating_the_code_stops_the_old_one_resolving()
        {
            var oldCode = await SeedClassroomAsync();
            var classroomId = (await NewContext().Classrooms.SingleAsync()).Id;

            var result = await NewService(NewContext()).RegenerateJoinCodeAsync(classroomId, _teacherId);

            Assert.Equal(ClassroomOutcome.Ok, result.Outcome);
            Assert.NotEqual(oldCode, result.JoinCode);
            Assert.Equal(JoinOutcome.NotFound, (await NewService(NewContext()).JoinAsync(oldCode, _studentId)).Outcome);
            Assert.Equal(JoinOutcome.Joined, (await NewService(NewContext()).JoinAsync(result.JoinCode!, _studentId)).Outcome);
        }

        [Fact]
        public async Task Leaving_removes_the_enrolment_and_repeating_it_is_safe()
        {
            var code = await SeedClassroomAsync();
            var joined = await NewService(NewContext()).JoinAsync(code, _studentId);

            await NewService(NewContext()).LeaveAsync(joined.ClassroomId, _studentId);
            await NewService(NewContext()).LeaveAsync(joined.ClassroomId, _studentId); // idempotent

            Assert.False(await NewContext().Enrollments.AnyAsync(e => e.StudentId == _studentId));
        }

        [Fact]
        public async Task The_owner_can_remove_a_student_and_the_roster_reflects_it()
        {
            var code = await SeedClassroomAsync();
            var joined = await NewService(NewContext()).JoinAsync(code, _studentId);

            var roster = await NewService(NewContext()).GetRosterAsync(joined.ClassroomId, _teacherId, 1, 20);
            Assert.NotNull(roster);
            Assert.Equal(1, roster!.Total);
            Assert.Equal(_studentId, roster.Items.Single().StudentId);

            var removed = await NewService(NewContext()).RemoveStudentAsync(joined.ClassroomId, _teacherId, _studentId);

            Assert.Equal(ClassroomOutcome.Ok, removed);
            var after = await NewService(NewContext()).GetRosterAsync(joined.ClassroomId, _teacherId, 1, 20);
            Assert.Equal(0, after!.Total);
        }

        [Fact]
        public async Task The_owned_dashboard_lists_only_your_own_classes_with_their_counts()
        {
            var code = await SeedClassroomAsync();
            await NewService(NewContext()).JoinAsync(code, _studentId);
            // Somebody else's class must never appear in this teacher's list.
            await NewService(NewContext()).CreateAsync(Guid.NewGuid(), "Another Teacher's Class");

            var owned = await NewService(NewContext()).GetOwnedAsync(_teacherId);

            Assert.Single(owned);
            Assert.Equal(1, owned[0].StudentCount);
            Assert.Equal(0, owned[0].QuizCount);
        }

        // ---- helpers ----

        /// <summary>Creates a classroom owned by the seed teacher and returns its join code.</summary>
        private async Task<string> SeedClassroomAsync()
        {
            var created = await NewService(NewContext()).CreateAsync(_teacherId, "Seeded Class");
            return created.Classroom!.JoinCode;
        }

        private async Task<(Guid classroomId, Guid quizId)> SeedClassroomWithPublishedQuizAsync()
        {
            var classroom = new Classroom(_teacherId, "Class With A Quiz");
            var quiz = new Quiz(classroom.Id, "Published Quiz", 10, _teacherId) { IsPublished = true };
            quiz.Questions.Add(new MultipleChoiceQuestion("2 + 2?", 10, new List<string> { "3", "4", "5" }, 1));

            await using var ctx = NewContext();
            ctx.Classrooms.Add(classroom);
            ctx.Quizzes.Add(quiz);
            ctx.Enrollments.Add(new Enrollment(_studentId, classroom.Id));
            await ctx.SaveChangesAsync();

            return (classroom.Id, quiz.Id);
        }

        // A fresh context per call, mirroring the per-request scope of the real app.
        private QuizDbContext NewContext() =>
            new(new DbContextOptionsBuilder<QuizDbContext>()
                .UseNpgsql(_postgres.GetConnectionString()).Options);

        private static ClassroomAppService NewService(QuizDbContext ctx) => new(new ClassroomRepository(ctx));

        private static TakeQuizFacade BuildFacade(QuizDbContext ctx) =>
            new(
                new QuizRepository(ctx),
                new QuizAttemptRepository(ctx),
                new StrategyFactory(),
                new QuizCommandInvoker(),
                new NoOpEventDispatcher());

        private sealed class NoOpEventDispatcher : IEventDispatcher
        {
            public Task DispatchAsync<TEvent>(TEvent domainEvent) => Task.CompletedTask;
        }
    }
}
