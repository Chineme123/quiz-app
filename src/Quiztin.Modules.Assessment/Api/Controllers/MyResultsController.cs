using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quiztin.Modules.Assessment.Application.Interfaces;

namespace Quiztin.Modules.Assessment.Api.Controllers
{
    /// <summary>
    /// A student's own results (spec 0011): every quiz they have finished, grouped by class, with a
    /// standing per class. Read only and scoped to the signed in student. The route takes no student
    /// id — the caller comes from the token — so another student's results are unreachable by
    /// construction (AC-1). An empty result is a 200 with no classrooms, never a 404, because a
    /// student always owns their own results (AC-8). The gate is authentication, not a role: whoever
    /// signs in sees their own attempts.
    /// </summary>
    [ApiController]
    [Route("api/results")]
    [Authorize]
    public class MyResultsController : AssessmentControllerBase
    {
        private readonly IMyResultsAppService _myResults;

        public MyResultsController(IMyResultsAppService myResults)
        {
            _myResults = myResults;
        }

        // GET /api/results/mine — the signed in student's own results, grouped by class (AC-1, AC-2).
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyResults()
        {
            var results = await _myResults.GetMyResultsAsync(GetCurrentUserId());
            return Ok(results);
        }
    }
}
