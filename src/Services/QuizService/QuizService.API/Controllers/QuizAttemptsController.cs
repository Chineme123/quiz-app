using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizService.Application.DTOs;
using QuizService.Application.Facades;
using QuizService.Application.Results;

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

        // GET /api/attempts/{attemptId}/questions — what the take screen renders (spec 0006,
        // AC-4). Read through the attempt, which is the authorisation: it is already owned by
        // one student and enrolment was checked when it started. The take screen therefore
        // never touches the unscoped GET /api/quizzes/{quizId}. No correct answers are returned.
        [HttpGet("{attemptId}/questions")]
        public async Task<IActionResult> GetQuestions(Guid attemptId)
        {
            var studentId = GetCurrentUserId();
            var questions = await _facade.GetAttemptQuestionsAsync(attemptId, studentId);
            if (questions is null) return NotFound();
            return Ok(questions);
        }

        // PUT /api/attempts/{attemptId}/answers — replaces the whole saved draft set in one
        // write (spec 0006, AC-6). Whole set, not per question, so two saves in flight cannot
        // interleave and drop an answer. 409 once the attempt is finished or its time is up,
        // which is what makes the deadline server enforced rather than client counted (AC-7).
        [HttpPut("{attemptId}/answers")]
        public async Task<IActionResult> SaveDraftAnswers(Guid attemptId, [FromBody] SaveDraftAnswersDto dto)
        {
            var studentId = GetCurrentUserId();
            var outcome = await _facade.SaveDraftAnswersAsync(attemptId, studentId, dto);
            return outcome switch
            {
                SaveDraftOutcome.NotFound => NotFound(),
                SaveDraftOutcome.Rejected => Conflict(new { error = "This attempt is no longer accepting answers." }),
                _ => NoContent()
            };
        }

        [HttpPost("{attemptId}/submit")]
        public async Task<IActionResult> SubmitQuiz(Guid attemptId, [FromBody] SubmitQuizDto dto)
        {
            var studentId = GetCurrentUserId();
            try
            {
                // The body carries only a CommandId: the drafts already saved are what gets
                // graded (spec 0006, AC-11). Scoped to the caller, so an attempt that isn't the
                // student's own (or doesn't exist) is 404 and never revealed (code-standards §5,
                // security.md §4/§7). A resubmit with a fresh CommandId is an idempotent no-op
                // returning the existing result; an attempt superseded by a newer one is a 409
                // rather than a raw 400 out of the domain (AC-16).
                var result = await _facade.SubmitQuizAsync(attemptId, studentId, dto);
                return result.Outcome switch
                {
                    SubmitQuizOutcome.NotFound => NotFound(),
                    SubmitQuizOutcome.Superseded =>
                        Conflict(new { error = "This attempt was superseded by a newer one and cannot be submitted." }),
                    _ => Ok(result.Result)
                };
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
