using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Interfaces;
using Quiztin.Modules.Assessment.Domain.Interfaces;
using Quiztin.Modules.Identity.Contracts;

namespace Quiztin.Modules.Assessment.Application.Services
{
    /// <summary>
    /// Teacher classroom results (spec 0010). Read only: it totals the existing QuizAttempt,
    /// QuizAnswer, and Enrollment data live, scoped to the owning teacher's classroom, and writes
    /// nothing (AC-11). Student names come from the Identity module's user directory, resolved for
    /// the page's ids only (AC-13). See IClassroomResultsAppService for the scoping rules.
    /// </summary>
    public class ClassroomResultsAppService : IClassroomResultsAppService
    {
        // Same page defaults as every other paged read in the module (spec 0008/0010 AC-10).
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 50;

        // The per-(student, quiz) cell states, shared by the wire contract and the frontend switch.
        private const string Completed = "Completed";
        private const string InProgress = "InProgress";
        private const string NotTaken = "NotTaken";

        private readonly IClassroomRepository _classrooms;
        private readonly IQuizRepository _quizzes;
        private readonly IResultsReadRepository _reads;
        private readonly IUserDirectory _users;

        public ClassroomResultsAppService(
            IClassroomRepository classrooms,
            IQuizRepository quizzes,
            IResultsReadRepository reads,
            IUserDirectory users)
        {
            _classrooms = classrooms;
            _quizzes = quizzes;
            _reads = reads;
            _users = users;
        }

        public async Task<ClassroomResultsSummaryDto?> GetClassroomSummaryAsync(Guid classroomId, Guid teacherId)
        {
            // Ownership first, before any student row is touched (AC-1). A classroom that is not
            // owned and one that does not exist are the same answer here, so 404 never leaks.
            var classroom = await _classrooms.GetByIdAsync(classroomId);
            if (classroom == null || classroom.TeacherId != teacherId) return null;

            var enrolled = await _reads.GetEnrolledStudentsAsync(classroomId);
            var studentIds = enrolled.Select(e => e.StudentId).ToList();

            var quizzes = await _quizzes.GetByClassroomAsync(classroomId);
            var attempted = (await _reads.GetQuizIdsWithAnyAttemptAsync(quizzes.Select(q => q.Id).ToList()))
                .ToHashSet();

            // Show a quiz once it is published or anyone has attempted it; a never-published draft
            // nobody has touched is not a result yet (AC-2).
            var shown = quizzes.Where(q => q.IsPublished || attempted.Contains(q.Id)).ToList();

            // The one shared read, reduced to the invariant set: the latest submitted attempt per
            // (student, quiz), over currently enrolled students only.
            var latest = ReduceToLatest(
                await _reads.GetSubmittedAttemptsAsync(shown.Select(q => q.Id).ToList(), studentIds));

            var rows = shown.Select(quiz =>
            {
                var totalPoints = quiz.Questions.Sum(qq => qq.Points);
                var scores = latest.Values.Where(r => r.QuizId == quiz.Id).ToList();

                decimal? averageScore = null;
                decimal? averagePercent = null;
                if (scores.Count > 0)
                {
                    var avg = scores.Average(r => r.Score ?? 0m);
                    averageScore = Math.Round(avg, 2);
                    averagePercent = totalPoints == 0 ? null : Math.Round(avg / totalPoints * 100m, 1);
                }

                return new QuizResultSummaryDto
                {
                    QuizId = quiz.Id,
                    Title = quiz.Title,
                    IsPublished = quiz.IsPublished,
                    TotalPoints = totalPoints,
                    CompletionCount = scores.Count,
                    AverageScore = averageScore,
                    AveragePercent = averagePercent
                };
            }).ToList();

            return new ClassroomResultsSummaryDto
            {
                ClassroomId = classroom.Id,
                ClassroomName = classroom.Name,
                IsArchived = classroom.ArchivedAt != null,
                StudentCount = enrolled.Count,
                Quizzes = rows
            };
        }

        public async Task<QuizResultsDto?> GetQuizResultsAsync(Guid quizId, Guid teacherId, int page, int pageSize)
        {
            // Ownership on the quiz's own CreatedByTeacherId (the field every quiz endpoint checks,
            // equal to the classroom's TeacherId). Non owner and not found are the same 404 (AC-1).
            var quiz = await _quizzes.GetByIdAsync(quizId);
            if (quiz == null || quiz.CreatedByTeacherId != teacherId) return null;

            var enrolled = await _reads.GetEnrolledStudentsAsync(quiz.ClassroomId);
            var studentIds = enrolled.Select(e => e.StudentId).ToList();
            var quizScope = new[] { quizId };

            var latest = ReduceToLatest(await _reads.GetSubmittedAttemptsAsync(quizScope, studentIds));
            var open = (await _reads.GetOpenAttemptsAsync(quizScope, studentIds))
                .Select(o => (o.StudentId, o.QuizId))
                .ToHashSet();

            var totalPoints = quiz.Questions.Sum(q => q.Points);
            var scores = latest.Values.ToList();
            var completionCount = scores.Count;

            decimal? averageScore = null;
            decimal? averagePercent = null;
            if (completionCount > 0)
            {
                var avg = scores.Average(r => r.Score ?? 0m);
                averageScore = Math.Round(avg, 2);
                averagePercent = totalPoints == 0 ? null : Math.Round(avg / totalPoints * 100m, 1);
            }

            // Per-question fraction correct over the latest submitted attempts only, so a retried
            // attempt never counts twice (AC-4). The denominator is the completion count: grading
            // wrote an answer row for every question of every submitted attempt.
            var correctByQuestion = (await _reads.GetAnswerCorrectnessAsync(scores.Select(r => r.AttemptId).ToList()))
                .GroupBy(c => c.QuestionId)
                .ToDictionary(g => g.Key, g => g.Count(c => c.IsCorrect));

            var questionRows = quiz.Questions.Select(q =>
            {
                var correct = correctByQuestion.TryGetValue(q.Id, out var c) ? c : 0;
                return new QuestionDifficultyDto
                {
                    QuestionId = q.Id,
                    Prompt = q.Prompt,
                    Points = q.Points,
                    CorrectCount = correct,
                    AnsweredCount = completionCount,
                    FractionCorrect = completionCount == 0
                        ? null
                        : Math.Round((decimal)correct / completionCount * 100m, 1)
                };
            }).ToList();

            var (safePage, safeSize, skip) = ClampPage(page, pageSize);
            var pageStudents = enrolled.Skip(skip).Take(safeSize).ToList();
            var names = await _users.GetByIdsAsync(pageStudents.Select(e => e.StudentId).ToList());

            var studentRows = pageStudents.Select(e =>
            {
                var (status, score, percent, attemptId) = CellFor(latest, open, e.StudentId, quizId, totalPoints);
                return new QuizStudentResultDto
                {
                    StudentId = e.StudentId,
                    DisplayName = ResolveName(names, e.StudentId),
                    Status = status,
                    Score = score,
                    Percent = percent,
                    AttemptId = attemptId
                };
            }).ToList();

            return new QuizResultsDto
            {
                QuizId = quiz.Id,
                ClassroomId = quiz.ClassroomId,
                Title = quiz.Title,
                TotalPoints = totalPoints,
                StudentCount = enrolled.Count,
                CompletionCount = completionCount,
                AverageScore = averageScore,
                AveragePercent = averagePercent,
                Questions = questionRows,
                Students = studentRows,
                Total = enrolled.Count,
                Page = safePage,
                PageSize = safeSize
            };
        }

        public async Task<StudentRollupDto?> GetStudentRollupAsync(Guid classroomId, Guid teacherId, int page, int pageSize)
        {
            var classroom = await _classrooms.GetByIdAsync(classroomId);
            if (classroom == null || classroom.TeacherId != teacherId) return null;

            var enrolled = await _reads.GetEnrolledStudentsAsync(classroomId);
            var studentIds = enrolled.Select(e => e.StudentId).ToList();

            var quizzes = await _quizzes.GetByClassroomAsync(classroomId);
            var attempted = (await _reads.GetQuizIdsWithAnyAttemptAsync(quizzes.Select(q => q.Id).ToList()))
                .ToHashSet();

            // Same quiz set as the summary (published or attempted): these are the roll-up columns.
            var shown = quizzes.Where(q => q.IsPublished || attempted.Contains(q.Id)).ToList();
            var totalPointsByQuiz = shown.ToDictionary(q => q.Id, q => q.Questions.Sum(qq => qq.Points));
            var shownIds = shown.Select(q => q.Id).ToList();

            var latest = ReduceToLatest(await _reads.GetSubmittedAttemptsAsync(shownIds, studentIds));
            var open = (await _reads.GetOpenAttemptsAsync(shownIds, studentIds))
                .Select(o => (o.StudentId, o.QuizId))
                .ToHashSet();

            var columns = shown.Select(q => new RollupQuizColumnDto
            {
                QuizId = q.Id,
                Title = q.Title,
                TotalPoints = totalPointsByQuiz[q.Id]
            }).ToList();

            var (safePage, safeSize, skip) = ClampPage(page, pageSize);
            var pageStudents = enrolled.Skip(skip).Take(safeSize).ToList();
            var names = await _users.GetByIdsAsync(pageStudents.Select(e => e.StudentId).ToList());

            var studentRows = pageStudents.Select(e =>
            {
                var cells = new List<StudentQuizScoreDto>(shown.Count);
                var takenPercents = new List<decimal>();

                foreach (var quiz in shown)
                {
                    var (status, score, percent, attemptId) =
                        CellFor(latest, open, e.StudentId, quiz.Id, totalPointsByQuiz[quiz.Id]);

                    cells.Add(new StudentQuizScoreDto
                    {
                        QuizId = quiz.Id,
                        Status = status,
                        Score = score,
                        Percent = percent,
                        AttemptId = attemptId
                    });

                    // The overall standing averages the taken quizzes' percentages (AC-5). A taken
                    // quiz with no points has an undefined percentage, so it is left out rather than
                    // counted as zero.
                    if (status == Completed && percent != null) takenPercents.Add(percent.Value);
                }

                return new StudentRollupRowDto
                {
                    StudentId = e.StudentId,
                    DisplayName = ResolveName(names, e.StudentId),
                    Scores = cells,
                    OverallStandingPercent = takenPercents.Count > 0
                        ? Math.Round(takenPercents.Average(), 1)
                        : null
                };
            }).ToList();

            return new StudentRollupDto
            {
                ClassroomId = classroom.Id,
                ClassroomName = classroom.Name,
                Quizzes = columns,
                Students = studentRows,
                Total = enrolled.Count,
                Page = safePage,
                PageSize = safeSize
            };
        }

        /// <summary>
        /// Reduces every submitted attempt to the one that counts per (student, quiz): the latest
        /// by SubmittedAt. Every aggregate is computed over this set, so a student who retried is
        /// counted once, not once per attempt (AC-7, the core invariant).
        /// </summary>
        private static Dictionary<(Guid StudentId, Guid QuizId), SubmittedAttemptRow> ReduceToLatest(
            IReadOnlyList<SubmittedAttemptRow> submitted)
        {
            return submitted
                .GroupBy(r => (r.StudentId, r.QuizId))
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(r => r.SubmittedAt).First());
        }

        /// <summary>
        /// The status and score cell for one (student, quiz): the latest submitted score wins
        /// even over a newer open attempt (AC-7, AC-8); In progress shows only with an open attempt
        /// and no submitted one; otherwise Not taken. Percentages normalize the score to the quiz's
        /// total points so cells compare across differently sized quizzes (AC-5).
        /// </summary>
        private static (string Status, decimal? Score, decimal? Percent, Guid? AttemptId) CellFor(
            IReadOnlyDictionary<(Guid StudentId, Guid QuizId), SubmittedAttemptRow> latest,
            IReadOnlySet<(Guid StudentId, Guid QuizId)> open,
            Guid studentId, Guid quizId, int totalPoints)
        {
            if (latest.TryGetValue((studentId, quizId), out var row))
            {
                var percent = (totalPoints == 0 || row.Score == null)
                    ? (decimal?)null
                    : Math.Round(row.Score.Value / totalPoints * 100m, 1);
                return (Completed, row.Score, percent, row.AttemptId);
            }

            return open.Contains((studentId, quizId))
                ? (InProgress, null, null, null)
                : (NotTaken, null, null, null);
        }

        /// <summary>The display name for a student id, falling back to the email when no display
        /// name is set, and to a generic label if the id resolves to no user at all — a row is
        /// never a bare id (AC-13).</summary>
        private static string ResolveName(IReadOnlyDictionary<Guid, UserIdentity> names, Guid studentId)
        {
            if (names.TryGetValue(studentId, out var identity))
            {
                return !string.IsNullOrWhiteSpace(identity.DisplayName) ? identity.DisplayName : identity.Email;
            }

            return "Unknown student";
        }

        private static (int Page, int PageSize, int Skip) ClampPage(int page, int pageSize)
        {
            var safePage = Math.Max(page, 1);
            var safeSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);
            return (safePage, safeSize, (safePage - 1) * safeSize);
        }
    }
}
