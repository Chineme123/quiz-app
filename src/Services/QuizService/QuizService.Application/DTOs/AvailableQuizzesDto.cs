using System;
using System.Collections.Generic;

namespace QuizService.Application.DTOs
{
    /// <summary>
    /// One page of the quizzes a student may take (spec 0006, AC-1). Paginated even though
    /// today's lists are small: an unpaginated list endpoint is a production incident waiting
    /// for the first busy classroom.
    /// </summary>
    public class AvailableQuizzesDto
    {
        public List<AvailableQuizDto> Items { get; set; } = new();

        /// <summary>Total matching quizzes, so the client can page without guessing.</summary>
        public int Total { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    /// <summary>
    /// A quiz as it appears in the student's list, with the one action that makes sense for it
    /// (spec 0006, AC-2). The open AttemptId rides along so Resume needs no extra round trip,
    /// and so the list cannot accidentally start a second attempt.
    /// </summary>
    public class AvailableQuizDto
    {
        public Guid QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int QuestionCount { get; set; }

        /// <summary>
        /// NotStarted (offer Start), InProgress (offer Resume), or Graded (offer View result).
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>The attempt to resume or to view, when there is one.</summary>
        public Guid? AttemptId { get; set; }
    }
}
