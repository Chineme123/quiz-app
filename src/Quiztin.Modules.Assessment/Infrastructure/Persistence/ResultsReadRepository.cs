using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Quiztin.Modules.Assessment.Domain.Interfaces;

namespace Quiztin.Modules.Assessment.Infrastructure.Persistence
{
    /// <summary>
    /// Read-only aggregation over the attempt tables for teacher classroom results (spec 0010).
    /// Queries project straight to projection rows (no entity hydration, no state rehydration, no
    /// tracking): these are reads, so they never load a QuizAttempt as a domain entity. The one
    /// shared read is "submitted attempts for a set of quizzes by a set of students"; the
    /// application layer reduces that to the latest per (student, quiz).
    /// </summary>
    public class ResultsReadRepository : IResultsReadRepository
    {
        private readonly QuizDbContext _context;

        public ResultsReadRepository(QuizDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<EnrolledStudent>> GetEnrolledStudentsAsync(Guid classroomId)
        {
            return await _context.Enrollments
                .Where(e => e.ClassroomId == classroomId)
                .OrderBy(e => e.EnrolledAt)
                .ThenBy(e => e.StudentId)
                .Select(e => new EnrolledStudent(e.StudentId, e.EnrolledAt))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Guid>> GetQuizIdsWithAnyAttemptAsync(IReadOnlyList<Guid> quizIds)
        {
            if (quizIds.Count == 0) return Array.Empty<Guid>();

            return await _context.QuizAttempts
                .Where(a => quizIds.Contains(a.QuizId))
                .Select(a => a.QuizId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<SubmittedAttemptRow>> GetSubmittedAttemptsAsync(
            IReadOnlyList<Guid> quizIds, IReadOnlyList<Guid> studentIds)
        {
            if (quizIds.Count == 0 || studentIds.Count == 0) return Array.Empty<SubmittedAttemptRow>();

            // Scoped to the classroom's own quizzes and its current roster (the ids passed in),
            // and to submitted attempts only. The reduction to "latest per (student, quiz)" is the
            // caller's, over this bounded set (classroom scale).
            return await _context.QuizAttempts
                .Where(a => a.SubmittedAt != null
                            && quizIds.Contains(a.QuizId)
                            && studentIds.Contains(a.StudentId))
                .Select(a => new SubmittedAttemptRow(a.StudentId, a.QuizId, a.Id, a.TotalScore, a.SubmittedAt))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<OpenAttemptRow>> GetOpenAttemptsAsync(
            IReadOnlyList<Guid> quizIds, IReadOnlyList<Guid> studentIds)
        {
            if (quizIds.Count == 0 || studentIds.Count == 0) return Array.Empty<OpenAttemptRow>();

            // Open = started, not submitted, not abandoned. A student holds at most one active
            // attempt per quiz, so Distinct is defensive; the anonymous projection keeps the
            // DISTINCT over plain columns (not a record constructor) for a clean translation.
            var rows = await _context.QuizAttempts
                .Where(a => a.SubmittedAt == null
                            && a.CurrentStateName != "Abandoned"
                            && quizIds.Contains(a.QuizId)
                            && studentIds.Contains(a.StudentId))
                .Select(a => new { a.StudentId, a.QuizId })
                .Distinct()
                .ToListAsync();

            return rows.Select(r => new OpenAttemptRow(r.StudentId, r.QuizId)).ToList();
        }

        public async Task<IReadOnlyList<AnswerCorrectnessRow>> GetAnswerCorrectnessAsync(IReadOnlyList<Guid> attemptIds)
        {
            if (attemptIds.Count == 0) return Array.Empty<AnswerCorrectnessRow>();

            return await _context.QuizAnswers
                .Where(ans => attemptIds.Contains(ans.QuizAttemptId))
                .Select(ans => new AnswerCorrectnessRow(ans.QuestionId, ans.IsCorrect))
                .ToListAsync();
        }
    }
}
