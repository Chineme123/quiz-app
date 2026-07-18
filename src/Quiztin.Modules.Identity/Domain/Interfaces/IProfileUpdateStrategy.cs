using Quiztin.Modules.Identity.Domain.Entities;
using Quiztin.Modules.Identity.Domain.Models;

namespace Quiztin.Modules.Identity.Domain.Interfaces
{
    public interface IProfileUpdateStrategy
    {
        bool Supports(string role);
        ValidationResult UpdateProfile(Profile profile, ProfileUpdateRequest request);
    }
}
