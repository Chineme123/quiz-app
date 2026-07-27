using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quiztin.Modules.Assessment.Application.Facades;
using Quiztin.Modules.Assessment.Application.Interfaces;

namespace Quiztin.Modules.Assessment.Api.Controllers
{
    /// <summary>
    /// Teacher classroom results (spec 0010): read-only aggregate views over the attempt data,
    /// every one scoped to the classroom or quiz the authenticated teacher owns. A non owner and a
    /// missing id both get 404 (the service returns null), so existence never leaks (AC-1).
    ///
    /// The gate is ownership, not the role: a signed-in student reaching a results URL is simply
    /// not the owner, so they get the same calm 404 as a missing class (AC-12), never a distinct
    /// "forbidden" — which is why there is no <c>[Authorize(Roles = "Teacher")]</c> here. Their own
    /// results are UC9.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize]
    public class ResultsController : AssessmentControllerBase
    {
        private readonly IClassroomResultsAppService _results;
        private readonly TakeQuizFacade _takeQuiz;

        public ResultsController(IClassroomResultsAppService results, TakeQuizFacade takeQuiz)
        {
            _results = results;
            _takeQuiz = takeQuiz;
        }

        // GET /api/classrooms/{classroomId}/results — the per-quiz summary, owner only (AC-2, AC-9).
        [HttpGet("classrooms/{classroomId:guid}/results")]
        public async Task<IActionResult> GetClassroomResults(Guid classroomId)
        {
            var summary = await _results.GetClassroomSummaryAsync(classroomId, GetCurrentUserId());
            return summary == null ? NotFound() : Ok(summary);
        }

        // GET /api/quizzes/{quizId}/results — per-question difficulty + paginated per-student rows,
        // owner only (AC-3, AC-4, AC-7, AC-8, AC-10).
        [HttpGet("quizzes/{quizId:guid}/results")]
        public async Task<IActionResult> GetQuizResults(Guid quizId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var results = await _results.GetQuizResultsAsync(quizId, GetCurrentUserId(), page, pageSize);
            return results == null ? NotFound() : Ok(results);
        }

        // GET /api/classrooms/{classroomId}/results/students — per-student roll-up with an overall
        // standing, owner only, paginated by student (AC-5, AC-10).
        [HttpGet("classrooms/{classroomId:guid}/results/students")]
        public async Task<IActionResult> GetStudentRollup(Guid classroomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var rollup = await _results.GetStudentRollupAsync(classroomId, GetCurrentUserId(), page, pageSize);
            return rollup == null ? NotFound() : Ok(rollup);
        }

        // GET /api/quizzes/{quizId}/results/students/{studentId} — drill-down into one student's
        // latest submitted attempt, owner only (AC-6). Reuses the student's own result computation
        // (the same per-question breakdown), reached through quiz ownership via the take-quiz facade.
        [HttpGet("quizzes/{quizId:guid}/results/students/{studentId:guid}")]
        public async Task<IActionResult> GetStudentAttempt(Guid quizId, Guid studentId)
        {
            var result = await _takeQuiz.GetOwnedStudentResultAsync(quizId, studentId, GetCurrentUserId());
            return result == null ? NotFound() : Ok(result);
        }
    }
}
