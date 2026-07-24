using System.Collections.Generic;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.Interfaces
{
    /// <summary>
    /// Produces candidate questions for a teacher to review (spec 0009). The real strategy calls
    /// the model; the deterministic one returns empty editable templates. Either way the output is
    /// a set of candidates that wait in a review batch, never questions added straight to the quiz.
    /// One strategy is selected at composition by the AiEnabled plus key gate, mirroring the
    /// feedback path.
    /// </summary>
    public interface IQuestionGenerationStrategy
    {
        Task<IReadOnlyList<GeneratedCandidate>> GenerateAsync(string topic, string difficulty, int count, string? sourceText = null);
    }
}
