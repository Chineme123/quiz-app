using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    /// <summary>
    /// A classroom's results at a glance (spec 0010, AC-2): one row per quiz that is published or
    /// has been attempted, with a completion count and the class average. Read only, computed on
    /// read; nothing here is stored. The wire contract for GET /api/classrooms/{id}/results.
    /// </summary>
    public class ClassroomResultsSummaryDto
    {
        public Guid ClassroomId { get; set; }
        public string ClassroomName { get; set; } = string.Empty;

        /// <summary>Archived classes still serve results, for the owner (AC-9); the flag lets the
        /// screen say so rather than hide the class.</summary>
        public bool IsArchived { get; set; }

        /// <summary>Currently enrolled students (the roster the averages count over).</summary>
        public int StudentCount { get; set; }

        public List<QuizResultSummaryDto> Quizzes { get; set; } = new();
    }

    public class QuizResultSummaryDto
    {
        public Guid QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsPublished { get; set; }

        /// <summary>The quiz's total points (sum of its questions), so the average reads against a
        /// scale. Zero for a quiz with no questions.</summary>
        public int TotalPoints { get; set; }

        /// <summary>Distinct currently enrolled students with a submitted attempt.</summary>
        public int CompletionCount { get; set; }

        /// <summary>Average of the latest submitted score per student, in points. Null when nobody
        /// has a submitted attempt (no average, never zero).</summary>
        public decimal? AverageScore { get; set; }

        /// <summary>The same average as a percentage of <see cref="TotalPoints"/>. Null when there
        /// is no average or the quiz has no points.</summary>
        public decimal? AveragePercent { get; set; }
    }
}
