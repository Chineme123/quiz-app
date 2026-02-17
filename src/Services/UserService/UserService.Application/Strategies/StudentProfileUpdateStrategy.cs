using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Domain.Models;

namespace UserService.Application.Strategies
{
    public class StudentProfileUpdateStrategy : IProfileUpdateStrategy
    {
        public bool Supports(string role) => role.Equals("Student", System.StringComparison.OrdinalIgnoreCase);

        public ValidationResult UpdateProfile(Profile profile, ProfileUpdateRequest request)
        {
            // 1. Apply core updates (common fields)
            profile.ApplyCoreUpdates(request);

            // 2. Validate core fields (e.g. DisplayName)
            var result = profile.ValidateCore();
            if (!result.IsSuccess) return result;

            // 3. Role-specific validation: AcademicLevel required for Student
            if (string.IsNullOrWhiteSpace(request.AcademicLevel))
            {
                result.AddError("AcademicLevel is required for students.");
                return result;
            }

            // 4. Apply role-specific updates
            profile.SetAcademicLevel(request.AcademicLevel);

            return result;
        }
    }
}
