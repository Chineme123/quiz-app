namespace UserService.Domain.Models
{
    public class ProfileUpdateRequest
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public string? School { get; set; }
        public string? Department { get; set; }
        
        // Role-specific fields (nullable, validation handles logic)
        public string? AcademicLevel { get; set; }
        public string? InstructorType { get; set; }
    }
}
