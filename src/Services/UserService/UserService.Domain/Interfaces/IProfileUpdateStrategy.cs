using UserService.Domain.Entities;
using UserService.Domain.Models;

namespace UserService.Domain.Interfaces
{
    public interface IProfileUpdateStrategy
    {
        bool Supports(string role);
        ValidationResult UpdateProfile(Profile profile, ProfileUpdateRequest request);
    }
}
