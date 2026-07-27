using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    /// <summary>
    /// One quiz's results for its owning teacher (spec 0010, AC-3/AC-4): the class average, a
    /// per-question difficulty breakdown, and a paginated per-student list showing each enrolled
    /// student's latest submitted score or a Not taken / In progress marker. The wire contract for
    /// GET /api/quizzes/{quizId}/results.
    /// </summary>
    public class QuizResultsDto
    {
        public Guid QuizId { get; set; }
        public Guid ClassroomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TotalPoints { get; set; }

        /// <summary>Currently enrolled students (also the pagination total).</summary>
        public int StudentCount { get; set; }

        /// <summary>Distinct enrolled students with a submitted attempt (the average's denominator
        /// and the per-question fraction's denominator).</summary>
        public int CompletionCount { get; set; }

        public decimal? AverageScore { get; set; }
        public decimal? AveragePercent { get; set; }

        /// <summary>Per-question difficulty; the whole question set, in every response (not paged).</summary>
        public List<QuestionDifficultyDto> Questions { get; set; } = new();

        /// <summary>The current page of per-student rows.</summary>
        public List<QuizStudentResultDto> Students { get; set; } = new();

        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class QuestionDifficultyDto
    {
        public Guid QuestionId { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public int Points { get; set; }

        /// <summary>Students (one per student, latest submitted) who answered this correctly.</summary>
        public int CorrectCount { get; set; }

        /// <summary>The denominator: the completion count. Zero means no one has finished, so there
        /// is no fraction yet.</summary>
        public int AnsweredCount { get; set; }

        /// <summary>Percentage who got it right, or null when no one has finished.</summary>
        public decimal? FractionCorrect { get; set; }
    }

    public class QuizStudentResultDto
    {
        public Guid StudentId { get; set; }

        /// <summary>The student's display name, never a bare id (AC-13).</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>"Completed", "InProgress", or "NotTaken".</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Latest submitted score in points; null unless Completed.</summary>
        public decimal? Score { get; set; }

        /// <summary>The same score as a percentage of <see cref="QuizResultsDto.TotalPoints"/>.</summary>
        public decimal? Percent { get; set; }

        /// <summary>The latest submitted attempt id, for the drill-down; null unless Completed.</summary>
        public Guid? AttemptId { get; set; }
    }
}
