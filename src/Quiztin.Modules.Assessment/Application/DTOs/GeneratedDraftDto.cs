using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    /// <summary>
    /// A quiz's pending review batch (spec 0009). An empty <see cref="Candidates"/> list with a
    /// null <see cref="CreatedAt"/> means the quiz has no pending batch.
    /// </summary>
    public class GeneratedDraftDto
    {
        public Guid QuizId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<DraftCandidateDto> Candidates { get; set; } = new();
    }

    /// <summary>One reviewable candidate. Its Id is what an accept request names to promote it.</summary>
    public class DraftCandidateDto
    {
        public Guid Id { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public int Points { get; set; }
        public List<string>? Options { get; set; }
        public int? CorrectOptionIndex { get; set; }
        public bool? CorrectAnswerBool { get; set; }
        public string? CorrectAnswerText { get; set; }
    }

    /// <summary>Which candidates from the pending batch to promote onto the quiz (spec 0009).</summary>
    public class AcceptDraftsDto
    {
        public List<Guid> DraftIds { get; set; } = new();
    }
}
