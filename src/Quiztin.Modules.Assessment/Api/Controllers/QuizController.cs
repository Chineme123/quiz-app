using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Facades;
using Quiztin.Modules.Assessment.Application.Interfaces;
using Quiztin.Modules.Assessment.Application.Results;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Quiztin.Modules.Assessment.Api.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize] // Require a valid JWT for all endpoints
    public class QuizController : AssessmentControllerBase
    {
        private readonly IQuizAppService _quizService;
        private readonly TakeQuizFacade _takeQuizFacade;

        public QuizController(IQuizAppService quizService, TakeQuizFacade takeQuizFacade)
        {
            _quizService = quizService;
            _takeQuizFacade = takeQuizFacade;
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

        // POST /api/quizzes/{quizId}/publish — the missing link that lets a quiz reach a
        // student (spec 0009, AC-1, AC-2). Owner only; a non owner gets 404, so a quiz's
        // existence never leaks. Publish is the first and only writer of the window and attempt
        // limit, and the take path already reads them, so nothing on the take side changes.
        [HttpPost("quizzes/{quizId}/publish")]
        public async Task<IActionResult> Publish(Guid quizId, [FromBody] PublishQuizDto request)
        {
            var result = await _quizService.PublishAsync(quizId, GetCurrentUserId(), request);
            return result.Outcome switch
            {
                PublishOutcome.NotFound => NotFound(),
                PublishOutcome.NoQuestions => BadRequest(new { error = "Add at least one question before publishing." }),
                PublishOutcome.InvalidWindow => BadRequest(new { error = "The 'available from' time must be before the 'available to' time." }),
                PublishOutcome.InvalidMaxAttempts => BadRequest(new { error = "A quiz must allow at least one attempt." }),
                _ => Ok(result.Quiz)
            };
        }

        // POST /api/quizzes/{quizId}/unpublish — take it back off the available list. Owner only.
        [HttpPost("quizzes/{quizId}/unpublish")]
        public async Task<IActionResult> Unpublish(Guid quizId)
        {
            var result = await _quizService.UnpublishAsync(quizId, GetCurrentUserId());
            return result.Outcome == PublishOutcome.NotFound ? NotFound() : Ok(result.Quiz);
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
