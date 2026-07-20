using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.Interfaces
{
    /// <summary>
    /// Classroom and enrolment persistence (spec 0008). A peer of IQuizAttemptRepository
    /// rather than more methods on IQuizRepository: classrooms are their own aggregate, and
    /// this slice adds enough operations that folding them in would bloat the quiz repo.
    ///
    /// The uniqueness rules live in the database (unique JoinCode, unique StudentId+ClassroomId),
    /// so the two Try* methods below report a constraint hit as a plain bool instead of letting a
    /// provider exception escape. That keeps the Postgres specifics in Infrastructure.
    /// </summary>
    public interface IClassroomRepository
    {
        Task<Classroom?> GetByIdAsync(Guid classroomId);

        /// <summary>Resolves a join code regardless of archived state; the caller decides.</summary>
        Task<Classroom?> GetByJoinCodeAsync(string joinCode);

        /// <summary>Inserts, regenerating the join code if it collides with an existing one.</summary>
        Task AddAsync(Classroom classroom);

        Task UpdateAsync(Classroom classroom);

        /// <summary>Issues a fresh unique join code, retrying on collision. Returns the new code.</summary>
        Task<string> RegenerateJoinCodeAsync(Classroom classroom);

        /// <summary>Classrooms this teacher owns, with the counts the dashboard shows.</summary>
        Task<IReadOnlyList<(Classroom Classroom, int StudentCount, int QuizCount)>> GetOwnedAsync(Guid teacherId);

        /// <summary>Active classrooms this user has joined. Archived ones drop off the list (AC-8).</summary>
        Task<IReadOnlyList<Classroom>> GetEnrolledAsync(Guid userId);

        Task<bool> IsEnrolledAsync(Guid userId, Guid classroomId);

        /// <summary>False when the unique index says this user is already enrolled (AC-3).</summary>
        Task<bool> TryAddEnrollmentAsync(Enrollment enrollment);

        /// <summary>False when there was nothing to remove, which is still a success to the caller.</summary>
        Task<bool> RemoveEnrollmentAsync(Guid userId, Guid classroomId);

        Task<(IReadOnlyList<Enrollment> Items, int Total)> GetRosterAsync(Guid classroomId, int skip, int take);
    }
}
