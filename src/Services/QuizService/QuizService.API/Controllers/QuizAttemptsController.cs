using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizService.Application.DTOs;
using QuizService.Application.Facades;

namespace QuizService.API.Controllers
{
    [ApiController]
    [Route("api/attempts")]
    [Authorize]
    public class QuizAttemptsController : ControllerBase
    {
        private readonly TakeQuizFacade _facade;

        public QuizAttemptsController(TakeQuizFacade facade)
        {
            _facade = facade;
        }

        private Guid GetCurrentUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(id, out var userId))
                throw new UnauthorizedAccessException("No valid user identity in the token.");
            return userId;
        }

        // POST /api/quizzes/{quizId}/start
        // Note: Route is slightly different to match standard resource but per requirement:
        // POST /api/quizzes/{quizId}/start
        [HttpPost("~/api/quizzes/{quizId}/start")] 
        public async Task<IActionResult> StartQuiz(Guid quizId)
        {
            var studentId = GetCurrentUserId();
            try
            {
                var attemptId = await _facade.StartQuizAsync(studentId, quizId);
                return CreatedAtAction(nameof(GetResult), new { attemptId }, new { attemptId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{attemptId}/submit")]
        public async Task<IActionResult> SubmitQuiz(Guid attemptId, [FromBody] SubmitQuizDto dto)
        {
            var studentId = GetCurrentUserId();
            try
            {
                // Scoped to the caller: submitting an attempt that isn't the student's own (or
                // one that doesn't exist) returns null -> 404, so it never reveals another
                // student's attempt (code-standards §5, security.md §4/§7). Otherwise returns
                // the graded result (score + per-question breakdown); a resubmit with a fresh
                // CommandId is an idempotent no-op that returns the existing result rather than
                // a 400 — see TakeQuizFacade.SubmitQuizAsync.
                var result = await _facade.SubmitQuizAsync(attemptId, studentId, dto);
                if (result is null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET /api/attempts/{attemptId}/result — the student's own result: score, feedback
        // status, and the per question breakdown, scoped to the caller (spec 0005, AC-8).
        // A request for someone else's attempt (or one that does not exist) returns 404,
        // so it never reveals another student's work (AC-9).
        [HttpGet("{attemptId}/result")]
        public async Task<IActionResult> GetResult(Guid attemptId)
        {
            var studentId = GetCurrentUserId();
            var result = await _facade.GetResultAsync(attemptId, studentId);
            if (result is null) return NotFound();
            return Ok(result);
        }
    }
}
