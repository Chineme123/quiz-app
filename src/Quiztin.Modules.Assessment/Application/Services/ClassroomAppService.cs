using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Application.DTOs;
using Quiztin.Modules.Assessment.Application.Interfaces;
using Quiztin.Modules.Assessment.Application.Results;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Interfaces;

namespace Quiztin.Modules.Assessment.Application.Services
{
    /// <summary>
    /// Classroom create, join, and management (spec 0008). See IClassroomAppService for the
    /// scoping and outcome rules this implements.
    /// </summary>
    public class ClassroomAppService : IClassroomAppService
    {
        public const int MaxNameLength = 100;

        // Same page defaults as the available quizzes list, so every paged read in the module
        // behaves identically (spec 0008 AC-10).
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 50;

        private readonly IClassroomRepository _classrooms;

        public ClassroomAppService(IClassroomRepository classrooms)
        {
            _classrooms = classrooms;
        }

        public async Task<CreateClassroomResult> CreateAsync(Guid teacherId, string name)
        {
            if (!TryNormalizeName(name, out var cleanName))
            {
                return new CreateClassroomResult { Outcome = ClassroomOutcome.InvalidName };
            }

            // The classroom is born with a join code (the constructor generates one); the
            // repository reissues it in the rare event the unique index rejects it.
            var classroom = new Classroom(teacherId, cleanName);
            await _classrooms.AddAsync(classroom);

            return new CreateClassroomResult
            {
                Outcome = ClassroomOutcome.Ok,
                Classroom = ToDto(classroom)
            };
        }

        public async Task<IReadOnlyList<OwnedClassroomDto>> GetOwnedAsync(Guid teacherId)
        {
            var rows = await _classrooms.GetOwnedAsync(teacherId);

            return rows.Select(r => new OwnedClassroomDto
            {
                Id = r.Classroom.Id,
                Name = r.Classroom.Name,
                JoinCode = r.Classroom.JoinCode,
                StudentCount = r.StudentCount,
                QuizCount = r.QuizCount,
                CreatedAt = r.Classroom.CreatedAt,
                ArchivedAt = r.Classroom.ArchivedAt
            }).ToList();
        }

        public async Task<IReadOnlyList<EnrolledClassroomDto>> GetEnrolledAsync(Guid userId)
        {
            var classrooms = await _classrooms.GetEnrolledAsync(userId);

            return classrooms.Select(c => new EnrolledClassroomDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();
        }

        public async Task<ClassroomDetailDto?> GetDetailAsync(Guid classroomId, Guid userId)
        {
            var classroom = await _classrooms.GetByIdAsync(classroomId);
            if (classroom == null) return null;

            if (classroom.TeacherId == userId)
            {
                // The owner view carries the join code and the counts; reuse the owned read so
                // the counts are computed the one way.
                var owned = await _classrooms.GetOwnedAsync(userId);
                var row = owned.FirstOrDefault(r => r.Classroom.Id == classroomId);

                return new ClassroomDetailDto
                {
                    Id = classroom.Id,
                    Name = classroom.Name,
                    IsOwner = true,
                    JoinCode = classroom.JoinCode,
                    ArchivedAt = classroom.ArchivedAt,
                    StudentCount = row.StudentCount,
                    QuizCount = row.QuizCount
                };
            }

            // A participant sees the name only, never the join code: holding a place in a class
            // is not permission to invite others into it.
            if (!await _classrooms.IsEnrolledAsync(userId, classroomId)) return null;

            return new ClassroomDetailDto
            {
                Id = classroom.Id,
                Name = classroom.Name,
                IsOwner = false
            };
        }

        public async Task<ClassroomRosterDto?> GetRosterAsync(Guid classroomId, Guid teacherId, int page, int pageSize)
        {
            if (await GetOwnedOrNullAsync(classroomId, teacherId) == null) return null;

            var safePage = Math.Max(page, 1);
            var safeSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

            var (items, total) = await _classrooms.GetRosterAsync(classroomId, (safePage - 1) * safeSize, safeSize);

            return new ClassroomRosterDto
            {
                Items = items.Select(e => new RosterEntryDto
                {
                    StudentId = e.StudentId,
                    EnrolledAt = e.EnrolledAt
                }).ToList(),
                Total = total,
                Page = safePage,
                PageSize = safeSize
            };
        }

        public async Task<JoinPreviewDto?> PreviewByCodeAsync(string joinCode, Guid userId)
        {
            var classroom = await GetJoinableByCodeAsync(joinCode);
            if (classroom == null) return null;

            return new JoinPreviewDto
            {
                ClassroomId = classroom.Id,
                Name = classroom.Name,
                AlreadyEnrolled = await _classrooms.IsEnrolledAsync(userId, classroom.Id),
                IsOwner = classroom.TeacherId == userId
            };
        }

        public async Task<JoinResult> JoinAsync(string joinCode, Guid userId)
        {
            var classroom = await GetJoinableByCodeAsync(joinCode);
            if (classroom == null) return JoinResult.Failed(JoinOutcome.NotFound);

            if (classroom.TeacherId == userId) return JoinResult.Failed(JoinOutcome.OwnClassroom);

            // No "am I already enrolled?" read first. Going straight at the insert and letting
            // the unique index arbitrate is what makes two concurrent joins safe: the loser is
            // reported as already enrolled, which is the same end state (AC-3).
            var added = await _classrooms.TryAddEnrollmentAsync(new Enrollment(userId, classroom.Id));

            return new JoinResult
            {
                Outcome = added ? JoinOutcome.Joined : JoinOutcome.AlreadyEnrolled,
                ClassroomId = classroom.Id,
                Name = classroom.Name
            };
        }

        public async Task LeaveAsync(Guid classroomId, Guid userId)
        {
            // Removing the enrolment only. Past attempts and their results are deliberately
            // untouched, so leaving never destroys a student's own history (AC-9).
            await _classrooms.RemoveEnrollmentAsync(userId, classroomId);
        }

        public async Task<ClassroomOutcome> RenameAsync(Guid classroomId, Guid teacherId, string name)
        {
            if (!TryNormalizeName(name, out var cleanName)) return ClassroomOutcome.InvalidName;

            var classroom = await GetOwnedOrNullAsync(classroomId, teacherId);
            if (classroom == null) return ClassroomOutcome.NotFound;

            classroom.Name = cleanName;
            await _classrooms.UpdateAsync(classroom);
            return ClassroomOutcome.Ok;
        }

        public async Task<ClassroomOutcome> ArchiveAsync(Guid classroomId, Guid teacherId)
        {
            var classroom = await GetOwnedOrNullAsync(classroomId, teacherId);
            if (classroom == null) return ClassroomOutcome.NotFound;

            // Archiving is reversible and deletes nothing: enrolments, quizzes, and every past
            // attempt survive. Already archived is a no op, so repeating it is safe (AC-8).
            classroom.ArchivedAt ??= DateTime.UtcNow;
            await _classrooms.UpdateAsync(classroom);
            return ClassroomOutcome.Ok;
        }

        public async Task<ClassroomOutcome> UnarchiveAsync(Guid classroomId, Guid teacherId)
        {
            var classroom = await GetOwnedOrNullAsync(classroomId, teacherId);
            if (classroom == null) return ClassroomOutcome.NotFound;

            classroom.ArchivedAt = null;
            await _classrooms.UpdateAsync(classroom);
            return ClassroomOutcome.Ok;
        }

        public async Task<RegenerateCodeResult> RegenerateJoinCodeAsync(Guid classroomId, Guid teacherId)
        {
            var classroom = await GetOwnedOrNullAsync(classroomId, teacherId);
            if (classroom == null) return new RegenerateCodeResult { Outcome = ClassroomOutcome.NotFound };

            // The old code stops resolving the moment this lands, which is the point: it is how
            // a teacher shuts off a code that leaked (AC-7).
            var joinCode = await _classrooms.RegenerateJoinCodeAsync(classroom);

            return new RegenerateCodeResult { Outcome = ClassroomOutcome.Ok, JoinCode = joinCode };
        }

        public async Task<ClassroomOutcome> RemoveStudentAsync(Guid classroomId, Guid teacherId, Guid studentId)
        {
            var classroom = await GetOwnedOrNullAsync(classroomId, teacherId);
            if (classroom == null) return ClassroomOutcome.NotFound;

            // Open join means a leaked code can enrol someone the teacher never invited, so the
            // owner needs a way to evict them. Their attempts and results are left intact.
            await _classrooms.RemoveEnrollmentAsync(studentId, classroomId);
            return ClassroomOutcome.Ok;
        }

        /// <summary>
        /// Loads a classroom only if this caller owns it. A classroom that does not exist and one
        /// owned by somebody else are deliberately indistinguishable here, so the caller can only
        /// ever report NotFound and existence never leaks (AC-7, AC-11).
        /// </summary>
        private async Task<Classroom?> GetOwnedOrNullAsync(Guid classroomId, Guid teacherId)
        {
            var classroom = await _classrooms.GetByIdAsync(classroomId);
            return classroom == null || classroom.TeacherId != teacherId ? null : classroom;
        }

        /// <summary>An archived class's code stops resolving, for preview and for join alike (AC-8).</summary>
        private async Task<Classroom?> GetJoinableByCodeAsync(string joinCode)
        {
            if (string.IsNullOrWhiteSpace(joinCode)) return null;

            var classroom = await _classrooms.GetByJoinCodeAsync(joinCode);
            return classroom == null || classroom.ArchivedAt != null ? null : classroom;
        }

        private static bool TryNormalizeName(string name, out string cleanName)
        {
            cleanName = (name ?? string.Empty).Trim();
            return cleanName.Length > 0 && cleanName.Length <= MaxNameLength;
        }

        private static ClassroomDto ToDto(Classroom classroom) => new()
        {
            Id = classroom.Id,
            Name = classroom.Name,
            JoinCode = classroom.JoinCode,
            CreatedAt = classroom.CreatedAt,
            ArchivedAt = classroom.ArchivedAt
        };
    }
}
