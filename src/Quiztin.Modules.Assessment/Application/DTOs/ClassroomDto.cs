using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    /// <summary>Request bodies for the classroom endpoints.</summary>
    public class CreateClassroomDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class RenameClassroomDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class JoinClassroomDto
    {
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>A classroom as its owning teacher sees it (spec 0008).</summary>
    public class ClassroomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string JoinCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
    }

    /// <summary>A row on the teacher dashboard: the class plus the counts it shows.</summary>
    public class OwnedClassroomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string JoinCode { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int QuizCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
    }

    /// <summary>
    /// A row on the student dashboard. Deliberately just the class name: the teacher's display
    /// name lives in the Identity module behind a plain Guid, so showing it would mean a cross
    /// module read (spec 0007). Tracked as a follow up on spec 0008.
    /// </summary>
    public class EnrolledClassroomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// One classroom in detail. The owner gets the join code and counts; an enrolled participant
    /// gets only the name, so the code is never handed to someone who cannot manage the class.
    /// </summary>
    public class ClassroomDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
        public string? JoinCode { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public int? StudentCount { get; set; }
        public int? QuizCount { get; set; }
    }

    /// <summary>What a student sees before committing to a join, resolved from the code.</summary>
    public class JoinPreviewDto
    {
        public Guid ClassroomId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool AlreadyEnrolled { get; set; }
        public bool IsOwner { get; set; }
    }

    /// <summary>
    /// The owner's roster view. Paged with the same default and cap as the available quizzes
    /// list (spec 0008 AC-10), since a class roster is the one list here that really grows.
    /// </summary>
    public class ClassroomRosterDto
    {
        public List<RosterEntryDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class RosterEntryDto
    {
        public Guid StudentId { get; set; }
        public DateTime EnrolledAt { get; set; }
    }
}
