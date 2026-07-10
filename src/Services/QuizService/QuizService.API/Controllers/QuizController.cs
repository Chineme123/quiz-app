using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizService.Application.DTOs;
using QuizService.Application.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuizService.API.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize] // Require a valid JWT for all endpoints
    public class QuizController : ControllerBase
    {
        private readonly IQuizAppService _quizService;

        public QuizController(IQuizAppService quizService)
        {
            _quizService = quizService;
        }

        // Canonical identity: the Guid from the JWT NameIdentifier claim. No fallback —
        // [Authorize] guarantees an authenticated principal; a missing/invalid id is a 403.
        private Guid GetCurrentTeacherId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(id, out var teacherId))
                throw new UnauthorizedAccessException("No valid user identity in the token.");
            return teacherId;
        }

        [HttpPost("classrooms/{classroomId}/quizzes")]
        public async Task<IActionResult> CreateQuiz(Guid classroomId, [FromBody] CreateQuizDto request)
        {
            var teacherId = GetCurrentTeacherId();
            try
            {
                var result = await _quizService.CreateQuizAsync(classroomId, teacherId, request);
                return CreatedAtAction(nameof(GetQuiz), new { quizId = result.Id }, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("quizzes/{quizId}/questions")]
        public async Task<IActionResult> AddQuestion(Guid quizId, [FromBody] AddQuestionDto request)
        {
            var teacherId = GetCurrentTeacherId();
            try
            {
                var result = await _quizService.AddQuestionAsync(quizId, teacherId, request);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("quizzes/{quizId}/generate")]
        public async Task<IActionResult> GenerateQuestions(Guid quizId, [FromBody] GenerateQuestionsDto request)
        {
            var teacherId = GetCurrentTeacherId();
             try
            {
                var result = await _quizService.GenerateQuestionsAsync(quizId, teacherId, request);
                return Ok(result);
            }
             catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("quizzes/{quizId}")]
        public async Task<IActionResult> GetQuiz(Guid quizId)
        {
            try
            {
                var result = await _quizService.GetQuizAsync(quizId);
                return Ok(result);
            }
             catch (System.Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
