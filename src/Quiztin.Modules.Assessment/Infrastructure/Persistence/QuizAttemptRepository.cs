using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Enums;
using Quiztin.Modules.Assessment.Domain.Interfaces;

namespace Quiztin.Modules.Assessment.Infrastructure.Persistence
{
    public class QuizAttemptRepository : IQuizAttemptRepository
    {
        private readonly QuizDbContext _context;

        public QuizAttemptRepository(QuizDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(QuizAttempt attempt)
        {
            await _context.QuizAttempts.AddAsync(attempt);
            await _context.SaveChangesAsync();
        }

        public async Task<QuizAttempt> GetByIdAsync(Guid id)
        {
            var attempt = await _context.QuizAttempts
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.Id == id);
                
            // Need to rehydrate state logic if loaded from DB
            if (attempt != null)
            {
                attempt.LoadState();
            }
            
            return attempt;
        }

        public async Task UpdateAsync(QuizAttempt attempt)
        {
            // The attempt is already tracked (loaded via GetByIdAsync on this same scoped
            // context), so its own change and any newly added answers are detected on save.
            // Do NOT call DbSet.Update here: it marks the whole graph Modified, which
            // mis-classifies freshly submitted answers (they carry client generated Guid
            // keys, so EF reads them as existing rows) and fails submit with a phantom
            // "affected 0 rows" concurrency conflict.
            await _context.SaveChangesAsync();
        }

        public async Task<QuizAttempt> FindByStudentAndQuizAsync(Guid studentId, Guid quizId)
        {
            var attempt = await _context.QuizAttempts
                .Include(a => a.Answers)
                .OrderByDescending(a => a.StartedAt) // Get latest if multiple allowed
                .FirstOrDefaultAsync(a => a.StudentId == studentId && a.QuizId == quizId);
            
             if (attempt != null)
            {
                attempt.LoadState();
            }
            
            return attempt;
        }

        public async Task<int> GetConsumedAttemptCountAsync(Guid studentId, Guid quizId)
        {
            // Only attempts that actually spent a try (spec 0006, AC-15). Counting every row
            // would lock a student out after one abandon; counting none of them would let them
            // restart forever and read the questions for free, so Superseded is the line.
            return await _context.QuizAttempts.CountAsync(a =>
                a.StudentId == studentId
                && a.QuizId == quizId
                && (a.CurrentStateName == "Submitted"
                    || a.CurrentStateName == "Graded"
                    || a.CurrentStateName == "Reviewable"
                    || (a.CurrentStateName == "Abandoned" && a.AbandonReason == AbandonReason.Superseded)));
        }

        public async Task<IReadOnlyList<QuizAttempt>> GetAttemptsForQuizzesAsync(Guid studentId, IReadOnlyList<Guid> quizIds)
        {
            // Scoped to the caller's own attempts: this feeds a list, and a list is exactly
            // where an unscoped query leaks someone else's work (code-standards §5).
            return await _context.QuizAttempts
                .Where(a => a.StudentId == studentId && quizIds.Contains(a.QuizId))
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();
        }

        public async Task<QuizAttempt?> GetInProgressAttemptAsync(Guid studentId, Guid quizId)
        {
            var attempt = await _context.QuizAttempts
                .FirstOrDefaultAsync(a =>
                    a.StudentId == studentId && a.QuizId == quizId && a.CurrentStateName == "InProgress");

            attempt?.LoadState();
            return attempt;
        }

        public async Task<bool> HasCommandBeenProcessedAsync(Guid commandId)
        {
            return await _context.ProcessedCommands.AnyAsync(c => c.CommandId == commandId);
        }

        public async Task MarkCommandAsProcessedAsync(Guid commandId, string type)
        {
            _context.ProcessedCommands.Add(new ProcessedCommand(commandId, type));
            await _context.SaveChangesAsync();
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            // Execution strategy owns the transaction so a transient-failure retry
            // re-runs the whole unit (EnableRetryOnFailure requirement).
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await operation();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
}
