using System;

namespace Quiztin.Modules.Assessment.Application.Results
{
    /// <summary>
    /// Outcome types for the classroom slice (spec 0008). The module has several failure
    /// styles; this one (an outcome enum the controller switches on, no try/catch) is chosen
    /// deliberately here because these endpoints span 400, 404, 409, and 204, and an enum maps
    /// each case to exactly one status without exceptions carrying control flow.
    ///
    /// Note what is NOT an outcome: a non owner acting on a classroom is reported as NotFound,
    /// never a distinct "forbidden", so existence never leaks across tenants (AC-7, AC-11).
    /// </summary>
    public enum ClassroomOutcome
    {
        Ok,
        NotFound,
        InvalidName
    }

    public enum JoinOutcome
    {
        /// <summary>A new enrolment was created.</summary>
        Joined,

        /// <summary>Already enrolled, including the loser of a concurrent join. Still success.</summary>
        AlreadyEnrolled,

        /// <summary>No classroom carries that code, or the one that does is archived.</summary>
        NotFound,

        /// <summary>You own this classroom, so you cannot also join it as a participant.</summary>
        OwnClassroom
    }

    public class JoinResult
    {
        public JoinOutcome Outcome { get; set; }
        public Guid ClassroomId { get; set; }
        public string Name { get; set; } = string.Empty;

        public static JoinResult Failed(JoinOutcome outcome) => new() { Outcome = outcome };
    }

    public class RegenerateCodeResult
    {
        public ClassroomOutcome Outcome { get; set; }
        public string? JoinCode { get; set; }
    }

    public class CreateClassroomResult
    {
        public ClassroomOutcome Outcome { get; set; }
        public DTOs.ClassroomDto? Classroom { get; set; }
    }
}
