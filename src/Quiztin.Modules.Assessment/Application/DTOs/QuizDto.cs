using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    public class QuizDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
        public Guid ClassroomId { get; set; }
        public Guid TeacherId { get; set; }

        // Publish state (spec 0009). Publish is the only writer of these; the take path
        // already reads them, so surfacing them here lets the authoring side see what a
        // publish did without a second round trip.
        public bool IsPublished { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableTo { get; set; }
        public int MaxAttempts { get; set; }

        // True once the quiz has any attempt (spec 0009, AC-9): its question set is frozen, so the
        // authoring UI shows the add, edit, and delete controls as locked. The publish settings
        // (window, attempts) may still change even when this is true.
        public bool IsLocked { get; set; }

        public List<QuestionDto> Questions { get; set; }
    }

    public class QuestionDto
    {
        public Guid Id { get; set; }
        /// <summary>"MultipleChoice", "TrueFalse", or "ShortAnswer": the same vocabulary an add or
        /// edit request uses, not the storage discriminator, so the wire has one set of names.</summary>
        public string QuestionType { get; set; }
        public string Prompt { get; set; }
        public int Points { get; set; }
        public List<string> Options { get; set; } // For MCQ

        // The correct answer, for the owner's editor (spec 0009, AC-3): a teacher cannot edit a
        // question without seeing which answer is right. Safe because QuizDto is returned only
        // from owner scoped authoring endpoints; the student take path uses AttemptQuestionsDto,
        // which never carries an answer (spec 0006).
        public int? CorrectOptionIndex { get; set; }
        public bool? CorrectAnswerBool { get; set; }
        public string? CorrectAnswerText { get; set; }
    }
}
