using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Interfaces;

namespace Quiztin.Modules.Assessment.Infrastructure.Persistence
{
    public class GeneratedQuestionDraftRepository : IGeneratedQuestionDraftRepository
    {
        private readonly QuizDbContext _context;

        public GeneratedQuestionDraftRepository(QuizDbContext context)
        {
            _context = context;
        }

        public async Task<GeneratedQuestionDraft?> GetByQuizAsync(Guid quizId)
        {
            return await _context.GeneratedQuestionDrafts.FirstOrDefaultAsync(d => d.QuizId == quizId);
        }

        public async Task<GeneratedQuestionDraft> UpsertAsync(Guid quizId, List<GeneratedCandidate> candidates)
        {
            // Reuse the one row if it exists, so regenerating replaces the batch rather than
            // racing a delete then insert against the unique QuizId index (spec 0009).
            var existing = await _context.GeneratedQuestionDrafts.FirstOrDefaultAsync(d => d.QuizId == quizId);
            if (existing is not null)
            {
                existing.Replace(candidates);
            }
            else
            {
                existing = new GeneratedQuestionDraft(quizId, candidates);
                await _context.GeneratedQuestionDrafts.AddAsync(existing);
            }
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAsync(GeneratedQuestionDraft draft)
        {
            // The draft is tracked on this same scoped context; if the caller has also added
            // questions to a tracked quiz (accept), this one save flushes both together.
            _context.GeneratedQuestionDrafts.Remove(draft);
            await _context.SaveChangesAsync();
        }
    }
}
