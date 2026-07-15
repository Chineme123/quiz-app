using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuizService.Domain.Entities;
using QuizService.Domain.Interfaces;

namespace QuizService.Infrastructure.Persistence
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

        public async Task<int> GetAttemptCountAsync(Guid studentId, Guid quizId)
        {
            return await _context.QuizAttempts.CountAsync(a => a.StudentId == studentId && a.QuizId == quizId);
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
