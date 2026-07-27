using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    /// <summary>
    /// The per-student roll-up for a classroom (spec 0010, AC-5): each enrolled student's score on
    /// each quiz plus an overall standing. Because quizzes have different point totals, each score
    /// is normalized to a percentage of that quiz's total, and the standing is the average of those
    /// percentages over the quizzes the student has taken, so different-sized quizzes compare
    /// fairly. Paginated by student. The wire contract for
    /// GET /api/classrooms/{id}/results/students.
    /// </summary>
    public class StudentRollupDto
    {
        public Guid ClassroomId { get; set; }
        public string ClassroomName { get; set; } = string.Empty;

        /// <summary>The quiz columns (published or attempted), same set as the summary. Each row's
        /// <see cref="StudentRollupRowDto.Scores"/> aligns with this list by QuizId.</summary>
        public List<RollupQuizColumnDto> Quizzes { get; set; } = new();

        /// <summary>The current page of student rows.</summary>
        public List<StudentRollupRowDto> Students { get; set; } = new();

        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class RollupQuizColumnDto
    {
        public Guid QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
    }

    public class StudentRollupRowDto
    {
        public Guid StudentId { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>One cell per quiz column, in the same order as <see cref="StudentRollupDto.Quizzes"/>.</summary>
        public List<StudentQuizScoreDto> Scores { get; set; } = new();

        /// <summary>Average of the taken quizzes' percentages; null when the student has taken
        /// none (excluded from the standing, not counted as zero).</summary>
        public decimal? OverallStandingPercent { get; set; }
    }

    public class StudentQuizScoreDto
    {
        public Guid QuizId { get; set; }

        /// <summary>"Completed", "InProgress", or "NotTaken".</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Latest submitted score in points; null unless Completed.</summary>
        public decimal? Score { get; set; }

        /// <summary>The same score as a percentage of the quiz's total points.</summary>
        public decimal? Percent { get; set; }

        /// <summary>The latest submitted attempt id, for the drill-down; null unless Completed.</summary>
        public Guid? AttemptId { get; set; }
    }
}
