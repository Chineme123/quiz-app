using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    /// <summary>
    /// A student's own results (spec 0011): every quiz they have finished, grouped by the class it
    /// belongs to, with a standing per class. Read only, computed on read; nothing here is stored.
    /// The wire contract for GET /api/results/mine. An empty Classrooms list is the calm empty
    /// state (the student has finished nothing), never a 404 — a student always owns their own
    /// results (AC-1, AC-8).
    /// </summary>
    public class MyResultsDto
    {
        /// <summary>One group per class the student is currently enrolled in, archived classes
        /// included (AC-7), so a class never silently disappears (AC-2).</summary>
        public List<MyResultsClassroomDto> Classrooms { get; set; } = new();
    }

    /// <summary>One classroom the student is enrolled in, with the quizzes they have finished there
    /// and how they are standing (AC-2, AC-6).</summary>
    public class MyResultsClassroomDto
    {
        public Guid ClassroomId { get; set; }
        public string ClassroomName { get; set; } = string.Empty;

        /// <summary>The class is archived but still shows the student's history (AC-7); the flag
        /// lets the screen say so rather than hide the class.</summary>
        public bool IsArchived { get; set; }

        /// <summary>The student's average percent over the quizzes they have finished in this class
        /// (AC-6). Null when they have finished none here, which the screen shows as a gentle note
        /// rather than a number.</summary>
        public decimal? StandingPercent { get; set; }

        /// <summary>The finished quizzes, newest first. Empty when the student has finished none in
        /// this class; the group still appears (AC-2).</summary>
        public List<MyResultsQuizDto> Quizzes { get; set; } = new();
    }

    /// <summary>One quiz the student has finished, showing the latest submitted attempt (AC-4). The
    /// AttemptId links to that attempt's per question detail, the existing results screen (AC-5).</summary>
    public class MyResultsQuizDto
    {
        public Guid QuizId { get; set; }
        public string Title { get; set; } = string.Empty;

        /// <summary>The quiz's total points (sum of its questions), so the score reads against a
        /// scale. Zero for a quiz with no questions.</summary>
        public int TotalPoints { get; set; }

        /// <summary>The latest submitted attempt's score, in points.</summary>
        public decimal? Score { get; set; }

        /// <summary>The same score as a percentage of <see cref="TotalPoints"/>; null when the quiz
        /// has no points.</summary>
        public decimal? Percent { get; set; }

        /// <summary>The latest submitted attempt's id, the link into its per question detail.</summary>
        public Guid AttemptId { get; set; }

        /// <summary>When that attempt was submitted (the row's date, and the newest first sort key).</summary>
        public DateTime SubmittedAt { get; set; }
    }
}
