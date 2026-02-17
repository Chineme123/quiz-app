using System;
using UserService.Domain.Models;

namespace UserService.Domain.Entities
{
    public class Profile
    {
        public Guid UserId { get; private set; }
        public string DisplayName { get; private set; } = string.Empty;
        public string? Bio { get; private set; }
        public string? AvatarUrl { get; private set; }
        public string? School { get; private set; }
        public string? Department { get; private set; }
        public string? AcademicLevel { get; private set; }
        public string? InstructorType { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public User User { get; private set; } = null!;

        private Profile() { } // EF Core

        public Profile(Guid userId)
        {
            if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ApplyCoreUpdates(ProfileUpdateRequest request)
        {
            DisplayName = request.DisplayName;
            Bio = request.Bio;
            AvatarUrl = request.AvatarUrl;
            School = request.School;
            Department = request.Department;
            
            // These might be set by strategies, but we allow direct setting if strategy calls this
            // However, normally strategies will set these specific properties directly or via specific methods
            // For now, let's keep it simple and assume strategies might modify the Profile *after* or *during* this call.
            // Actually, per prompt: "Profile must implement ... ApplyCoreUpdates(ProfileUpdateRequest request)"
            // "Strategy validates ... role-specific invariants."
            
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetAcademicLevel(string? academicLevel)
        {
            AcademicLevel = academicLevel;
        }

        public void SetInstructorType(string? instructorType)
        {
            InstructorType = instructorType;
        }

        public ValidationResult ValidateCore()
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                result.AddError("DisplayName cannot be empty.");
            }

            return result;
        }
    }
}
