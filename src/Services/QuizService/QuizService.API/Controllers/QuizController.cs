using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizService.Application.DTOs;
using QuizService.Application.Facades;
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
        private readonly TakeQuizFacade _takeQuizFacade;

        public QuizController(IQuizAppService quizService, TakeQuizFacade takeQuizFacade)
        {
            _quizService = quizService;
            _takeQuizFacade = takeQuizFacade;
        }

        // Canonical identity: the Guid from the JWT NameIdentifier claim. No fallback —
        // [Authorize] guarantees an authenticated principal; a missing/invalid id is a 403.
        // One identity for every caller: the same claim answers "which teacher" for authoring
        // and "which student" for the available list, so it is named for neither (§4).
        private Guid GetCurrentUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(id, out var userId))
                throw new UnauthorizedAccessException("No valid user identity in the token.");
            return userId;
        }

        [HttpPost("classrooms/{classroomId}/quizzes")]
        public async Task<IActionResult> CreateQuiz(Guid classroomId, [FromBody] CreateQuizDto request)
        {
            var teacherId = GetCurrentUserId();
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
            var teacherId = GetCurrentUserId();
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
            var teacherId = GetCurrentUserId();
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

        // GET /api/quizzes/available — the quizzes the caller may take (spec 0006, AC-1, AC-2).
        // Scoped to the authenticated student: published, in window, and only in classrooms
        // they are enrolled in. Paginated with a default size and a hard cap. Unlike GetQuiz
        // below, this one is scoped, which is why the take screen uses it and not that.
        [HttpGet("quizzes/available")]
        public async Task<IActionResult> GetAvailableQuizzes([FromQuery] int page = 1, [FromQuery] int pageSize = 0)
        {
            var studentId = GetCurrentUserId();
            var result = await _takeQuizFacade.GetAvailableQuizzesAsync(studentId, page, pageSize);
            return Ok(result);
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
