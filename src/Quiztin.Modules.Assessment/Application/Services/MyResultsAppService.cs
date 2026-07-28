using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Interfaces;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Interfaces;

namespace Quiztin.Modules.Assessment.Application.Services
{
    /// <summary>
    /// A student's own results (spec 0011). Read only: it totals the existing QuizAttempt and Quiz
    /// data live, scoped to the signed in student, and writes nothing (AC-10). Simpler than the
    /// teacher view (spec 0010): the data is the caller's own, so there is no cross module name
    /// lookup and no ownership 404 — an empty result is a normal empty payload (AC-1, AC-8).
    /// </summary>
    public class MyResultsAppService : IMyResultsAppService
    {
        private readonly IClassroomRepository _classrooms;
        private readonly IQuizRepository _quizzes;
        private readonly IResultsReadRepository _reads;

        public MyResultsAppService(
            IClassroomRepository classrooms,
            IQuizRepository quizzes,
            IResultsReadRepository reads)
        {
            _classrooms = classrooms;
            _quizzes = quizzes;
            _reads = reads;
        }

        public async Task<MyResultsDto> GetMyResultsAsync(Guid studentId)
        {
            // Every class the student is enrolled in, archived included (AC-7). Deliberately NOT
            // GetEnrolledAsync, which drops archived classes for the active list (spec 0008 AC-8):
            // a student's graded work in an archived class is still theirs to review here.
            var classrooms = await _classrooms.GetEnrolledForResultsAsync(studentId);
            if (classrooms.Count == 0) return new MyResultsDto();

            // The quizzes in each class (one read per class; a handful of classes, so the N+1 is
            // fine at this scale). Reading by classroom keeps the quiz set scoped to classes the
            // student is actually enrolled in.
            var quizzesByClassroom = new Dictionary<Guid, IReadOnlyList<Quiz>>();
            foreach (var classroom in classrooms)
            {
                quizzesByClassroom[classroom.Id] = await _quizzes.GetByClassroomAsync(classroom.Id);
            }

            // The student's latest submitted attempt per quiz across all their classes. Passing only
            // this student's id keeps the read scoped to the caller (AC-1); ReduceToLatest collapses
            // a retried quiz to the one attempt that counts (AC-4).
            var allQuizIds = quizzesByClassroom.Values
                .SelectMany(quizzes => quizzes.Select(q => q.Id))
                .ToList();
            var latest = ResultsCalculations.ReduceToLatest(
                await _reads.GetSubmittedAttemptsAsync(allQuizIds, new[] { studentId }));

            var groups = classrooms.Select(classroom =>
            {
                var rows = new List<MyResultsQuizDto>();
                var takenPercents = new List<decimal>();

                foreach (var quiz in quizzesByClassroom[classroom.Id])
                {
                    // Only quizzes the student has finished appear (AC-3): a quiz with no latest
                    // submitted attempt by this student is skipped and stays in the available list.
                    if (!latest.TryGetValue((studentId, quiz.Id), out var row)) continue;
                    if (row.SubmittedAt is not { } submittedAt) continue;

                    var totalPoints = quiz.Questions.Sum(q => q.Points);
                    var percent = ResultsCalculations.Percent(row.Score, totalPoints);

                    rows.Add(new MyResultsQuizDto
                    {
                        QuizId = quiz.Id,
                        Title = quiz.Title,
                        TotalPoints = totalPoints,
                        Score = row.Score,
                        Percent = percent,
                        AttemptId = row.AttemptId,
                        SubmittedAt = submittedAt
                    });

                    // The standing averages the finished quizzes' percentages (AC-6). A finished
                    // quiz with no points has an undefined percentage, so it is left out rather than
                    // counted as zero.
                    if (percent is { } p) takenPercents.Add(p);
                }

                return new MyResultsClassroomDto
                {
                    ClassroomId = classroom.Id,
                    ClassroomName = classroom.Name,
                    IsArchived = classroom.ArchivedAt != null,
                    StandingPercent = takenPercents.Count > 0
                        ? Math.Round(takenPercents.Average(), 1)
                        : null,
                    // Newest finished first within a class.
                    Quizzes = rows.OrderByDescending(r => r.SubmittedAt).ToList()
                };
            }).ToList();

            return new MyResultsDto { Classrooms = groups };
        }
    }
}
