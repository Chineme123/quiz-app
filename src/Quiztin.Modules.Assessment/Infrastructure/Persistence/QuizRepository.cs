using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Interfaces;

namespace Quiztin.Modules.Assessment.Infrastructure.Persistence
{
    public class QuizRepository : IQuizRepository
    {
        private readonly QuizDbContext _context;

        public QuizRepository(QuizDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Quiz quiz)
        {
            await _context.Quizzes.AddAsync(quiz);
            await _context.SaveChangesAsync();
        }

        public async Task<Quiz?> GetByIdAsync(Guid id)
        {
            return await _context.Quizzes
                         .Include(q => q.Questions)
                         .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task UpdateAsync(Quiz quiz)
        {
            // The quiz is already tracked (loaded via GetByIdAsync on this same scoped
            // context), so its own change and any newly added questions are detected on save.
            // Do NOT call DbSet.Update here: it marks the whole graph Modified, which
            // mis-classifies freshly added questions (they carry client generated Guid keys,
            // so EF reads them as existing rows) and fails the add with a phantom
            // "affected 0 rows" concurrency conflict. Same fix as QuizAttemptRepository.UpdateAsync.
            await _context.SaveChangesAsync();
        }

        public async Task<Classroom?> GetClassroomAsync(Guid classroomId)
        {
            return await _context.Classrooms.FindAsync(classroomId);
        }

        public async Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid classroomId)
        {
            // FR7: only students enrolled in the classroom may take its quizzes.
            return await _context.Enrollments
                .AnyAsync(e => e.StudentId == studentId && e.ClassroomId == classroomId);
        }

        public async Task<(IReadOnlyList<Quiz> Items, int Total)> GetAvailableForStudentAsync(Guid studentId, int skip, int take)
        {
            var now = DateTime.UtcNow;

            // The enrolment subquery IS the tenant scope (FR7, code-standards §5): a quiz the
            // student is not enrolled for can never appear, whatever they pass. The window and
            // published checks mirror Quiz.CanStart, so the list only offers what Start accepts.
            // The archived check is the other half of that mirror (spec 0008, AC-8): archiving a
            // classroom must stop its quizzes listing as well as starting, and Quiz has no
            // Classroom navigation, so it reads as a subquery like the enrolment one above.
            var available = _context.Quizzes
                .Where(q => q.IsPublished
                            && (q.AvailableFrom == null || q.AvailableFrom <= now)
                            && (q.AvailableTo == null || q.AvailableTo >= now)
                            && _context.Classrooms.Any(c => c.Id == q.ClassroomId && c.ArchivedAt == null)
                            && _context.Enrollments.Any(e => e.StudentId == studentId && e.ClassroomId == q.ClassroomId));

            var total = await available.CountAsync();
            var items = await available
                .OrderBy(q => q.Title)
                .Skip(skip)
                .Take(take)
                .Include(q => q.Questions)
                .ToListAsync();

            return (items, total);
        }
    }
}
