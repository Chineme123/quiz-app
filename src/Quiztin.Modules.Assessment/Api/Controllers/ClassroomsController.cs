using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Interfaces;
using Quiztin.Modules.Assessment.Application.Results;

namespace Quiztin.Modules.Assessment.Api.Controllers
{
    /// <summary>
    /// Classroom create, join, and management (spec 0008). Two authorization rules run here:
    /// creating a classroom is gated on the Teacher role (read straight from the JWT role
    /// claim), while joining is open to any authenticated user because the join code is itself
    /// the capability. Everything else is owner scoped in the service, which reports a non owner
    /// as NotFound so a classroom's existence never leaks (AC-7, AC-11).
    /// </summary>
    [ApiController]
    [Route("api/classrooms")]
    [Authorize]
    public class ClassroomsController : AssessmentControllerBase
    {
        private readonly IClassroomAppService _classrooms;

        public ClassroomsController(IClassroomAppService classrooms)
        {
            _classrooms = classrooms;
        }

        // POST /api/classrooms — only a Teacher may own a classroom (AC-1).
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Create([FromBody] CreateClassroomDto request)
        {
            var result = await _classrooms.CreateAsync(GetCurrentUserId(), request.Name);

            return result.Outcome switch
            {
                ClassroomOutcome.InvalidName =>
                    BadRequest(new { error = "A class name is required and must be 100 characters or fewer." }),
                _ => CreatedAtAction(nameof(GetDetail), new { id = result.Classroom!.Id }, result.Classroom)
            };
        }

        // GET /api/classrooms/owned — the teacher dashboard (AC-2).
        [HttpGet("owned")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetOwned()
        {
            return Ok(await _classrooms.GetOwnedAsync(GetCurrentUserId()));
        }

        // GET /api/classrooms/enrolled — the student dashboard (AC-5).
        [HttpGet("enrolled")]
        public async Task<IActionResult> GetEnrolled()
        {
            return Ok(await _classrooms.GetEnrolledAsync(GetCurrentUserId()));
        }

        // GET /api/classrooms/{id} — owner or enrolled participant; anyone else gets 404.
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var detail = await _classrooms.GetDetailAsync(id, GetCurrentUserId());
            return detail == null ? NotFound() : Ok(detail);
        }

        // GET /api/classrooms/{id}/students — owner only, paged (AC-10).
        [HttpGet("{id:guid}/students")]
        public async Task<IActionResult> GetRoster(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var roster = await _classrooms.GetRosterAsync(id, GetCurrentUserId(), page, pageSize);
            return roster == null ? NotFound() : Ok(roster);
        }

        // DELETE /api/classrooms/{id}/students/{studentId} — the owner evicting someone who
        // joined with a leaked or mistyped code. Idempotent, and it removes only the enrolment:
        // the student's past attempts and results are untouched (AC-7).
        [HttpDelete("{id:guid}/students/{studentId:guid}")]
        public async Task<IActionResult> RemoveStudent(Guid id, Guid studentId)
        {
            var outcome = await _classrooms.RemoveStudentAsync(id, GetCurrentUserId(), studentId);
            return outcome == ClassroomOutcome.NotFound ? NotFound() : NoContent();
        }

        // GET /api/classrooms/by-code/{code} — what the join screen shows before the student
        // commits, so a link never enrols anyone silently (AC-4).
        [HttpGet("by-code/{code}")]
        public async Task<IActionResult> PreviewByCode(string code)
        {
            var preview = await _classrooms.PreviewByCodeAsync(code, GetCurrentUserId());
            return preview == null ? NotFound() : Ok(preview);
        }

        // POST /api/classrooms/join — open to any authenticated user; the code is the gate.
        // Already enrolled is a success, not a conflict, so a double submit is harmless (AC-3).
        [HttpPost("join")]
        public async Task<IActionResult> Join([FromBody] JoinClassroomDto request)
        {
            var result = await _classrooms.JoinAsync(request.Code, GetCurrentUserId());

            return result.Outcome switch
            {
                JoinOutcome.NotFound => NotFound(new { error = "No class matches that code." }),
                JoinOutcome.OwnClassroom =>
                    Conflict(new { error = "You teach this class, so you cannot join it as a student." }),
                _ => Ok(new { classroomId = result.ClassroomId, name = result.Name })
            };
        }

        // POST /api/classrooms/{id}/leave — idempotent; leaving keeps past results (AC-9).
        [HttpPost("{id:guid}/leave")]
        public async Task<IActionResult> Leave(Guid id)
        {
            await _classrooms.LeaveAsync(id, GetCurrentUserId());
            return NoContent();
        }

        // PATCH /api/classrooms/{id} — rename, owner only.
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Rename(Guid id, [FromBody] RenameClassroomDto request)
        {
            var outcome = await _classrooms.RenameAsync(id, GetCurrentUserId(), request.Name);

            return outcome switch
            {
                ClassroomOutcome.NotFound => NotFound(),
                ClassroomOutcome.InvalidName =>
                    BadRequest(new { error = "A class name is required and must be 100 characters or fewer." }),
                _ => Ok(new { id, name = request.Name.Trim() })
            };
        }

        // POST /api/classrooms/{id}/archive — reversible, destroys nothing (AC-8).
        [HttpPost("{id:guid}/archive")]
        public async Task<IActionResult> Archive(Guid id)
        {
            var outcome = await _classrooms.ArchiveAsync(id, GetCurrentUserId());
            if (outcome == ClassroomOutcome.NotFound) return NotFound();

            var detail = await _classrooms.GetDetailAsync(id, GetCurrentUserId());
            return Ok(new { id, archivedAt = detail?.ArchivedAt });
        }

        [HttpPost("{id:guid}/unarchive")]
        public async Task<IActionResult> Unarchive(Guid id)
        {
            var outcome = await _classrooms.UnarchiveAsync(id, GetCurrentUserId());
            return outcome == ClassroomOutcome.NotFound ? NotFound() : Ok(new { id });
        }

        // POST /api/classrooms/{id}/regenerate-code — the old code and its link stop working
        // immediately, which is how a teacher shuts off a code that leaked (AC-7).
        [HttpPost("{id:guid}/regenerate-code")]
        public async Task<IActionResult> RegenerateCode(Guid id)
        {
            var result = await _classrooms.RegenerateJoinCodeAsync(id, GetCurrentUserId());

            return result.Outcome == ClassroomOutcome.NotFound
                ? NotFound()
                : Ok(new { id, joinCode = result.JoinCode });
        }
    }
}
