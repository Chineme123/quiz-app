using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Domain.Entities
{
    /// <summary>
    /// A pending batch of candidate questions a teacher generated and has not yet reviewed
    /// (spec 0009). One row per quiz (a UNIQUE index on QuizId makes that a database invariant),
    /// so regenerating replaces the batch rather than piling up. The candidates live in one jsonb
    /// column, the same way MultipleChoiceQuestion.Options and the attempt's draft answers do.
    /// The batch is always pending: presence is the only state. Accepting promotes chosen
    /// candidates onto the quiz and deletes the row; discarding just deletes it.
    /// </summary>
    public class GeneratedQuestionDraft
    {
        public Guid Id { get; set; }
        public Guid QuizId { get; set; }
        public List<GeneratedCandidate> Candidates { get; set; } = new();
        public DateTime CreatedAt { get; set; }

        public GeneratedQuestionDraft(Guid quizId, List<GeneratedCandidate> candidates)
        {
            Id = Guid.NewGuid();
            QuizId = quizId;
            Candidates = candidates;
            CreatedAt = DateTime.UtcNow;
        }

        // Host for EF
        protected GeneratedQuestionDraft() { }

        /// <summary>Replaces the whole candidate set, for a regenerate that reuses the row.</summary>
        public void Replace(List<GeneratedCandidate> candidates)
        {
            Candidates = candidates;
            CreatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// One reviewable candidate inside a <see cref="GeneratedQuestionDraft"/> (spec 0009). Its
    /// fields mirror an authored question so accepting it runs through the same
    /// QuestionFactory.TryCreate validation a manual add does. The Id lets an accept request name
    /// exactly which candidates to promote. Stored as jsonb, so it carries public setters.
    /// </summary>
    public class GeneratedCandidate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string QuestionType { get; set; } = string.Empty; // "MultipleChoice", "TrueFalse", "ShortAnswer"
        public string Prompt { get; set; } = string.Empty;
        public int Points { get; set; }

        public List<string>? Options { get; set; }
        public int? CorrectOptionIndex { get; set; }
        public bool? CorrectAnswerBool { get; set; }
        public string? CorrectAnswerText { get; set; }
    }
}
