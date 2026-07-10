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
            try
            {
                await _facade.SubmitQuizAsync(attemptId, dto);
                return Ok(new { message = "Quiz submitted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{attemptId}/result")]
        public async Task<IActionResult> GetResult(Guid attemptId)
        {
            try
            {
                var attempt = await _facade.GetReviewAsync(attemptId);
                return Ok(new 
                { 
                    attempt.Id, 
                    attempt.QuizId, 
                    attempt.TotalScore, 
                    Status = attempt.CurrentStateName 
                });
            }
            catch (Exception ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("{attemptId}/review")]
        public async Task<IActionResult> GetReview(Guid attemptId)
        {
            try
            {
                var attempt = await _facade.GetReviewAsync(attemptId);
                // Return DTO with feedback
                return Ok(attempt); // Direct generic serialization for now, ideally map to DTO
            }
            catch (Exception ex)
            {
                 return NotFound(new { error = ex.Message });
            }
        }
    }
}
