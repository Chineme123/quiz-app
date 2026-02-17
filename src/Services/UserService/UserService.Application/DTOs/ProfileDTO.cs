using System;

namespace UserService.Application.DTOs
{
    public class ProfileDTO
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public string? School { get; set; }
        public string? Department { get; set; }
        public string? AcademicLevel { get; set; }
        public string? InstructorType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
