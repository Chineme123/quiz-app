using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Results;

namespace Quiztin.Modules.Assessment.Application.Interfaces
{
    /// <summary>
    /// Classroom create, join, and management (spec 0008). Every method takes the caller's
    /// Guid id as a parameter: the controller reads it from the JWT NameIdentifier claim and
    /// passes it in, so no query here is ever scoped by a client supplied id (AC-11).
    ///
    /// Owner scoped operations report a non owner as NotFound rather than a distinct forbidden
    /// outcome, so a classroom's existence never leaks to someone who does not own it (AC-7).
    /// The role gate (only a Teacher may create) is an authorization concern and lives on the
    /// controller, not here.
    /// </summary>
    public interface IClassroomAppService
    {
        Task<CreateClassroomResult> CreateAsync(Guid teacherId, string name);

        Task<IReadOnlyList<OwnedClassroomDto>> GetOwnedAsync(Guid teacherId);

        Task<IReadOnlyList<EnrolledClassroomDto>> GetEnrolledAsync(Guid userId);

        /// <summary>Null when the caller neither owns nor is enrolled in the classroom.</summary>
        Task<ClassroomDetailDto?> GetDetailAsync(Guid classroomId, Guid userId);

        /// <summary>Null when the caller does not own the classroom.</summary>
        Task<ClassroomRosterDto?> GetRosterAsync(Guid classroomId, Guid teacherId, int page, int pageSize);

        /// <summary>Null when no active classroom carries that code.</summary>
        Task<JoinPreviewDto?> PreviewByCodeAsync(string joinCode, Guid userId);

        Task<JoinResult> JoinAsync(string joinCode, Guid userId);

        /// <summary>Idempotent: leaving a class you are not in is still a success.</summary>
        Task LeaveAsync(Guid classroomId, Guid userId);

        Task<ClassroomOutcome> RenameAsync(Guid classroomId, Guid teacherId, string name);

        Task<ClassroomOutcome> ArchiveAsync(Guid classroomId, Guid teacherId);

        Task<ClassroomOutcome> UnarchiveAsync(Guid classroomId, Guid teacherId);

        Task<RegenerateCodeResult> RegenerateJoinCodeAsync(Guid classroomId, Guid teacherId);

        /// <summary>Idempotent: removing someone who is not enrolled is still a success.</summary>
        Task<ClassroomOutcome> RemoveStudentAsync(Guid classroomId, Guid teacherId, Guid studentId);
    }
}
