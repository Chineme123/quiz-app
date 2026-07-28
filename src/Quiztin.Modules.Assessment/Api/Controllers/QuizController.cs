using System;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quiztin.Modules.Assessment.Api.Filters;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Facades;
using Quiztin.Modules.Assessment.Application.Interfaces;
using Quiztin.Modules.Assessment.Application.Results;
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

        // The source upload size cap (spec 0009, AC-7). An oversized upload is rejected with 413
        // before the action buffers it: the RejectOversizedUpload filter on the generate endpoint
        // checks Content-Length before binding, with the request-size attributes as a backstop.
        // Kept in step with GenerationOptions.MaxUploadBytes.
        private const long SourceUploadByteCap = 5 * 1024 * 1024;

        public QuizController(IQuizAppService quizService, TakeQuizFacade takeQuizFacade)
        {
            _quizService = quizService;
            _takeQuizFacade = takeQuizFacade;
        }

        // POST /api/classrooms/{classroomId}/quizzes — create a quiz in a class you own
        // (spec 0009, AC-3). A non owner or a missing class both get 404, so a class's
        // existence never leaks.
        [HttpPost("classrooms/{classroomId}/quizzes")]
        public async Task<IActionResult> CreateQuiz(Guid classroomId, [FromBody] CreateQuizDto request)
        {
            var result = await _quizService.CreateQuizAsync(classroomId, GetCurrentUserId(), request);
            // Ok always carries the created quiz; a null quiz means a failure outcome below.
            if (result.Quiz is { } created)
                return CreatedAtAction(nameof(GetQuiz), new { quizId = created.Id }, created);
            return MapAuthoring(result, () => NoContent());
        }

        // GET /api/classrooms/{classroomId}/quizzes — the teacher's quizzes in one class
        // they own (spec 0009, AC-10): title, publish state, question count, attempt count.
        [HttpGet("classrooms/{classroomId}/quizzes")]
        public async Task<IActionResult> GetClassroomQuizzes(Guid classroomId)
        {
            var quizzes = await _quizService.GetQuizzesForClassroomAsync(classroomId, GetCurrentUserId());
            return quizzes is null ? NotFound() : Ok(quizzes);
        }

        // POST /api/quizzes/{quizId}/questions — add a question by hand (spec 0009, AC-3).
        // 400 for a malformed question, 404 for a non owner, 409 once the quiz is attempted.
        [HttpPost("quizzes/{quizId}/questions")]
        public async Task<IActionResult> AddQuestion(Guid quizId, [FromBody] AddQuestionDto request)
        {
            var result = await _quizService.AddQuestionAsync(quizId, GetCurrentUserId(), request);
            return MapAuthoring(result, () => Ok(result.Quiz));
        }

        // PATCH /api/quizzes/{quizId}/questions/{questionId} — edit a question in place
        // (spec 0009, AC-3). The type cannot change here; that is a delete then add.
        [HttpPatch("quizzes/{quizId}/questions/{questionId}")]
        public async Task<IActionResult> EditQuestion(Guid quizId, Guid questionId, [FromBody] AddQuestionDto request)
        {
            var result = await _quizService.EditQuestionAsync(quizId, questionId, GetCurrentUserId(), request);
            return MapAuthoring(result, () => Ok(result.Quiz));
        }

        // DELETE /api/quizzes/{quizId}/questions/{questionId} — remove a question
        // (spec 0009, AC-3). 404 for a non owner or a missing question, 409 once attempted.
        [HttpDelete("quizzes/{quizId}/questions/{questionId}")]
        public async Task<IActionResult> DeleteQuestion(Guid quizId, Guid questionId)
        {
            var result = await _quizService.DeleteQuestionAsync(quizId, questionId, GetCurrentUserId());
            return MapAuthoring(result, () => NoContent());
        }

        // POST /api/quizzes/{quizId}/generate — multipart/form-data (spec 0009, AC-4, AC-7). Form
        // fields topic, difficulty, count, and optional sourceText, plus an optional file part
        // (PDF or docx). An oversized upload is rejected with 413 before the action buffers it: the
        // RejectOversizedUpload filter checks Content-Length before binding (multipart binding on its
        // own would surface a 400), and the two request-size attributes backstop a body streamed with
        // no Content-Length. The batch is real claude-opus-4-8 output when generation is on and a key
        // is present, else empty editable templates. Owner only (404); 409 once attempted; 400 for a
        // blank topic or an unreadable file; 415 for a file whose real content is not PDF or docx.
        [HttpPost("quizzes/{quizId}/generate")]
        [RejectOversizedUpload(SourceUploadByteCap)]
        [RequestSizeLimit(SourceUploadByteCap)]
        [RequestFormLimits(MultipartBodyLengthLimit = SourceUploadByteCap)]
        public async Task<IActionResult> GenerateQuestions(Guid quizId, [FromForm] GenerateQuestionsDto request, IFormFile? file)
        {
            byte[]? fileBytes = null;
            if (file is { Length: > 0 })
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            var result = await _quizService.GenerateQuestionsAsync(quizId, GetCurrentUserId(), request, fileBytes);
            return result.Outcome switch
            {
                GenerationOutcome.NotFound => NotFound(),
                GenerationOutcome.Locked => Conflict(new { error = "This quiz has been attempted, so its questions can no longer be changed." }),
                GenerationOutcome.UnsupportedType => StatusCode(StatusCodes.Status415UnsupportedMediaType, new { error = result.Error }),
                GenerationOutcome.Invalid => BadRequest(new { error = result.Error }),
                _ => Ok(result.Draft)
            };
        }

        // GET /api/quizzes/{quizId}/drafts — the pending review batch, or an empty batch. Owner only.
        [HttpGet("quizzes/{quizId}/drafts")]
        public async Task<IActionResult> GetDrafts(Guid quizId)
        {
            var draft = await _quizService.GetDraftsAsync(quizId, GetCurrentUserId());
            return draft is null ? NotFound() : Ok(draft);
        }

        // POST /api/quizzes/{quizId}/drafts/accept — promote the chosen candidates onto the quiz
        // and clear the batch (spec 0009, AC-8). Owner only; 409 once attempted; 400 if a chosen
        // candidate is not a valid question (for example an unfilled template).
        [HttpPost("quizzes/{quizId}/drafts/accept")]
        public async Task<IActionResult> AcceptDrafts(Guid quizId, [FromBody] AcceptDraftsDto request)
        {
            var result = await _quizService.AcceptDraftsAsync(quizId, GetCurrentUserId(), request);
            return MapAuthoring(result, () => Ok(result.Quiz));
        }

        // POST /api/quizzes/{quizId}/drafts/discard — throw the pending batch away. Owner only.
        [HttpPost("quizzes/{quizId}/drafts/discard")]
        public async Task<IActionResult> DiscardDrafts(Guid quizId)
        {
            var result = await _quizService.DiscardDraftsAsync(quizId, GetCurrentUserId());
            return MapAuthoring(result, () => NoContent());
        }

        // GET /api/quizzes/available — the quizzes the caller may take (spec 0006, AC-1, AC-2).
        // Scoped to the authenticated student: published, in window, and only in classrooms
        // they are enrolled in. Paginated with a default size and a hard cap.
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

        // GET /api/quizzes/{quizId} — the full quiz for editing (spec 0009, AC-10): questions,
        // settings, publish state, and the locked flag. Owner scoped: a non owner gets 404, so a
        // quiz's existence never leaks. This replaced the old unscoped read.
        [HttpGet("quizzes/{quizId}")]
        public async Task<IActionResult> GetQuiz(Guid quizId)
        {
            var quiz = await _quizService.GetQuizForEditingAsync(quizId, GetCurrentUserId());
            return quiz is null ? NotFound() : Ok(quiz);
        }

        // Maps an authoring outcome to a status. NotFound (non owner or missing) is 404 so a
        // quiz's existence never leaks; Locked (the quiz has an attempt) is 409; Invalid carries
        // its reasons in the house { error } envelope so the client raises a clean error. Each
        // caller supplies its own success result (a created, an Ok body, or a 204).
        private IActionResult MapAuthoring(AuthoringResult result, Func<IActionResult> onOk)
            => result.Outcome switch
            {
                AuthoringOutcome.Ok => onOk(),
                AuthoringOutcome.NotFound => NotFound(),
                AuthoringOutcome.Locked => Conflict(new { error = "This quiz has been attempted, so its questions can no longer be changed." }),
                AuthoringOutcome.Invalid => BadRequest(new { error = string.Join(" ", result.Errors) }),
                _ => StatusCode(500)
            };
    }
}
