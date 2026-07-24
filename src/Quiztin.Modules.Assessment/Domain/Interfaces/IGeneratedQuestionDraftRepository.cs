using System;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.Interfaces
{
    /// <summary>
    /// The one pending draft batch per quiz (spec 0009). Backed by a UNIQUE index on QuizId, so
    /// at most one batch exists; <see cref="UpsertAsync"/> reuses the row rather than racing a
    /// delete then insert.
    /// </summary>
    public interface IGeneratedQuestionDraftRepository
    {
        /// <summary>The quiz's pending batch, or null if it has none.</summary>
        Task<GeneratedQuestionDraft?> GetByQuizAsync(Guid quizId);

        /// <summary>Stores the batch for a quiz, replacing the candidates on the existing row if
        /// one is already there. Returns the stored batch.</summary>
        Task<GeneratedQuestionDraft> UpsertAsync(Guid quizId, System.Collections.Generic.List<GeneratedCandidate> candidates);

        /// <summary>Deletes a pending batch. Used by accept (after promoting) and discard.</summary>
        Task DeleteAsync(GeneratedQuestionDraft draft);
    }
}
