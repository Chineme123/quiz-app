using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Interfaces;
using Quiztin.Modules.Assessment.Domain.Services;

namespace Quiztin.Modules.Assessment.Infrastructure.Persistence
{
    public class ClassroomRepository : IClassroomRepository
    {
        // Postgres unique_violation. The database, not a read-then-write check, is what makes
        // join idempotent and join codes unique, so both races land here as a caught constraint
        // hit rather than a 500 (spec 0008).
        private const string UniqueViolation = "23505";
        private const string JoinCodeIndex = "IX_Classrooms_JoinCode";

        // A collision on a 31^6 code space is vanishingly rare, so a few tries is plenty; the
        // loop exists so the rare case degrades into a retry instead of an error.
        private const int JoinCodeAttempts = 5;

        private readonly QuizDbContext _context;

        public ClassroomRepository(QuizDbContext context)
        {
            _context = context;
        }

        public async Task<Classroom?> GetByIdAsync(Guid classroomId)
        {
            return await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == classroomId);
        }

        public async Task<Classroom?> GetByJoinCodeAsync(string joinCode)
        {
            // Codes are stored uppercase; compare on the normalized form so a student typing
            // lowercase still finds the class.
            var normalized = joinCode.Trim().ToUpperInvariant();
            return await _context.Classrooms.FirstOrDefaultAsync(c => c.JoinCode == normalized);
        }

        public async Task AddAsync(Classroom classroom)
        {
            _context.Classrooms.Add(classroom);
            await SaveRetryingJoinCodeAsync(classroom);
        }

        public async Task UpdateAsync(Classroom classroom)
        {
            _context.Classrooms.Update(classroom);
            await _context.SaveChangesAsync();
        }

        public async Task<string> RegenerateJoinCodeAsync(Classroom classroom)
        {
            classroom.JoinCode = JoinCodeGenerator.Generate();
            _context.Classrooms.Update(classroom);
            await SaveRetryingJoinCodeAsync(classroom);
            return classroom.JoinCode;
        }

        /// <summary>
        /// Saves, and on a join code collision issues a new code and tries again. Any other
        /// constraint failure is a real error and propagates.
        /// </summary>
        private async Task SaveRetryingJoinCodeAsync(Classroom classroom)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return;
                }
                catch (DbUpdateException ex)
                    when (attempt < JoinCodeAttempts && IsJoinCodeCollision(ex))
                {
                    classroom.JoinCode = JoinCodeGenerator.Generate();
                }
            }
        }

        private static bool IsJoinCodeCollision(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: UniqueViolation } pg
            && pg.ConstraintName == JoinCodeIndex;

        public async Task<IReadOnlyList<(Classroom Classroom, int StudentCount, int QuizCount)>> GetOwnedAsync(Guid teacherId)
        {
            // Scoped by the owner id the caller was authenticated as (AC-11). Counts are computed
            // at read time rather than stored, so they can never go stale.
            var rows = await _context.Classrooms
                .Where(c => c.TeacherId == teacherId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    Classroom = c,
                    StudentCount = _context.Enrollments.Count(e => e.ClassroomId == c.Id),
                    QuizCount = _context.Quizzes.Count(q => q.ClassroomId == c.Id)
                })
                .ToListAsync();

            return rows
                .Select(r => (r.Classroom, r.StudentCount, r.QuizCount))
                .ToList();
        }

        public async Task<IReadOnlyList<Classroom>> GetEnrolledAsync(Guid userId)
        {
            // Archived classes drop off a student's list (AC-8) while their enrolment and any
            // past attempts survive untouched.
            return await _context.Classrooms
                .Where(c => c.ArchivedAt == null
                            && _context.Enrollments.Any(e => e.ClassroomId == c.Id && e.StudentId == userId))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<bool> IsEnrolledAsync(Guid userId, Guid classroomId)
        {
            return await _context.Enrollments
                .AnyAsync(e => e.StudentId == userId && e.ClassroomId == classroomId);
        }

        public async Task<bool> TryAddEnrollmentAsync(Enrollment enrollment)
        {
            _context.Enrollments.Add(enrollment);
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException { SqlState: UniqueViolation })
            {
                // Two joins raced for the same (StudentId, ClassroomId) and this one lost. The
                // winner already produced the row, so the end state is exactly what the caller
                // wanted: enrolled. Report it as "already enrolled", never a 500 (AC-3).
                _context.Entry(enrollment).State = EntityState.Detached;
                return false;
            }
        }

        public async Task<bool> RemoveEnrollmentAsync(Guid userId, Guid classroomId)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == userId && e.ClassroomId == classroomId);

            if (enrollment == null) return false;

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(IReadOnlyList<Enrollment> Items, int Total)> GetRosterAsync(Guid classroomId, int skip, int take)
        {
            var roster = _context.Enrollments.Where(e => e.ClassroomId == classroomId);

            var total = await roster.CountAsync();
            var items = await roster
                .OrderBy(e => e.EnrolledAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, total);
        }
    }
}
