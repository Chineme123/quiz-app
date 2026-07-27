using System;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Application.DTOs;

namespace Quiztin.Modules.Assessment.Application.Interfaces
{
    /// <summary>
    /// Teacher classroom results (spec 0010): read-only aggregate views over the existing attempt
    /// data, every one scoped to the classroom (or quiz) the authenticated teacher owns. A non
    /// owner and a missing id are indistinguishable — both return null, which the controller maps
    /// to 404, so existence never leaks (AC-1, code-standards §5).
    /// </summary>
    public interface IClassroomResultsAppService
    {
        /// <summary>
        /// The per-quiz summary for a classroom the teacher owns: each published-or-attempted quiz
        /// with its completion count and class average. Null if the caller does not own the
        /// classroom or it does not exist. Served for archived classrooms too (AC-2, AC-9).
        /// </summary>
        Task<ClassroomResultsSummaryDto?> GetClassroomSummaryAsync(Guid classroomId, Guid teacherId);

        /// <summary>
        /// One quiz's results for its owning teacher: per-question difficulty plus a paginated
        /// per-student list (latest submitted score, or Not taken / In progress). Null if the
        /// caller does not own the quiz or it does not exist (AC-3, AC-4, AC-7, AC-8, AC-10).
        /// </summary>
        Task<QuizResultsDto?> GetQuizResultsAsync(Guid quizId, Guid teacherId, int page, int pageSize);

        /// <summary>
        /// The per-student roll-up for a classroom the teacher owns: each enrolled student's score
        /// per quiz plus a percentage-normalized overall standing, paginated by student. Null if
        /// the caller does not own the classroom or it does not exist (AC-5, AC-10).
        /// </summary>
        Task<StudentRollupDto?> GetStudentRollupAsync(Guid classroomId, Guid teacherId, int page, int pageSize);
    }
}
