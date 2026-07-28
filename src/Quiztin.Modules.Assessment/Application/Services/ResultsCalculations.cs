using System;
using System.Collections.Generic;
using System.Linq;
using Quiztin.Modules.Assessment.Domain.Interfaces;

namespace Quiztin.Modules.Assessment.Application.Services
{
    /// <summary>
    /// The two calculations both results views share: the teacher's classroom results (spec 0010)
    /// and the student's own results (spec 0011). These were private helpers on
    /// ClassroomResultsAppService; extracting them keeps the two views from drifting on the rules
    /// that must agree, namely which attempt counts and how a score becomes a percentage.
    /// </summary>
    internal static class ResultsCalculations
    {
        /// <summary>
        /// Reduces every submitted attempt to the one that counts per (student, quiz): the latest
        /// by SubmittedAt. Every aggregate is computed over this set, so a student who retried is
        /// counted once, not once per attempt (spec 0010 AC-7, spec 0011 AC-4).
        /// </summary>
        public static Dictionary<(Guid StudentId, Guid QuizId), SubmittedAttemptRow> ReduceToLatest(
            IReadOnlyList<SubmittedAttemptRow> submitted)
        {
            return submitted
                .GroupBy(r => (r.StudentId, r.QuizId))
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(r => r.SubmittedAt).First());
        }

        /// <summary>
        /// A score as a percentage of the quiz's total points, rounded to one decimal place. Null
        /// when the score is unknown or the quiz has no points, which guards the divide and keeps
        /// differently sized quizzes comparable (spec 0010 AC-5, spec 0011 AC-6).
        /// </summary>
        public static decimal? Percent(decimal? score, int totalPoints)
        {
            return (totalPoints == 0 || score == null)
                ? null
                : Math.Round(score.Value / totalPoints * 100m, 1);
        }
    }
}
