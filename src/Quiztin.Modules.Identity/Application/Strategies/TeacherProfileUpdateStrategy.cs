using Quiztin.Modules.Identity.Domain.Entities;
using Quiztin.Modules.Identity.Domain.Interfaces;
using Quiztin.Modules.Identity.Domain.Models;

namespace Quiztin.Modules.Identity.Application.Strategies
{
    public class TeacherProfileUpdateStrategy : IProfileUpdateStrategy
    {
        public bool Supports(string role) => role.Equals("Teacher", System.StringComparison.OrdinalIgnoreCase);

        public ValidationResult UpdateProfile(Profile profile, ProfileUpdateRequest request)
        {
            // 1. Apply core updates
            profile.ApplyCoreUpdates(request);

            // 2. Validate core
            var result = profile.ValidateCore();
            if (!result.IsSuccess) return result;

            // 3. Role-specific validation: InstructorType required for Teacher
            if (string.IsNullOrWhiteSpace(request.InstructorType))
            {
                result.AddError("InstructorType is required for teachers.");
                return result;
            }

            // 4. Apply role-specific updates
            profile.SetInstructorType(request.InstructorType);

            return result;
        }
    }
}
