using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    /// <summary>
    /// A student's own attempt result (spec 0005, AC-8): the score, the feedback status,
    /// and a per question breakdown. The wire contract for GET /api/attempts/{id}/result.
    /// </summary>
    public class AttemptResultDto
    {
        public Guid AttemptId { get; set; }
        public Guid QuizId { get; set; }
        public decimal? TotalScore { get; set; }

        /// <summary>"Pending" until background feedback finishes, then "Ready" (AC-7).</summary>
        public string FeedbackStatus { get; set; } = string.Empty;

        /// <summary>The attempt lifecycle state (e.g. "Graded", "Reviewable").</summary>
        public string Status { get; set; } = string.Empty;

        public List<AttemptAnswerResultDto> Answers { get; set; } = new();
    }

    public class AttemptAnswerResultDto
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string ProvidedAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public decimal PointsAwarded { get; set; }
        public string? Feedback { get; set; }

        /// <summary>"Ai" or "Deterministic"; null until feedback is written.</summary>
        public string? FeedbackSource { get; set; }
    }
}
