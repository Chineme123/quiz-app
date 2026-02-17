using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Domain.Models;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    // We will assume the existence of a way to get UserID and Role
    public class UserProfileController : ControllerBase
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IEnumerable<IProfileUpdateStrategy> _strategies;

        public UserProfileController(IProfileRepository profileRepository, IEnumerable<IProfileUpdateStrategy> strategies)
        {
            _profileRepository = profileRepository;
            _strategies = strategies;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var profile = await _profileRepository.GetByUserIdAsync(userId);
            if (profile == null) return NotFound("Profile not found.");

            return Ok(MapToDTO(profile));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var role = GetUserRole();
            if (string.IsNullOrEmpty(role)) return Forbid(); // Or Unauthorized

            // 1. Select Strategy
            var strategy = _strategies.FirstOrDefault(s => s.Supports(role));
            if (strategy == null)
            {
                return BadRequest($"No profile strategy found for role: {role}");
            }

            // 2. Retrieve or Create Profile
            var profile = await _profileRepository.GetByUserIdAsync(userId);
            bool isNew = false;
            if (profile == null)
            {
                profile = new Profile(userId);
                isNew = true;
            }

            // 3. Apply Updates via Strategy
            var validationResult = strategy.UpdateProfile(profile, request);

            // 4. Handle Validation Failure
            if (!validationResult.IsSuccess)
            {
                return BadRequest(validationResult.Errors);
            }

            // 5. Persist
            if (isNew)
            {
                await _profileRepository.AddAsync(profile);
            }
            else
            {
                await _profileRepository.UpdateAsync(profile);
            }

            try
            {
                await _profileRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, "An error occurred while saving the profile.");
            }

            return Ok(MapToDTO(profile));
        }

        // Helpers to extract claims - in a real app, this might be in a base controller or service
        private Guid GetUserId()
        {
            // For development/testing without full Auth interop, we might look for a specific header or claim
            // Standard implementations use User.FindFirst
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(idClaim, out var userId)) return userId;
            
            // Fallback for testing if allowed, or just return Empty
            return Guid.Empty;
        }

        private string? GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        private ProfileDTO MapToDTO(Profile profile)
        {
            return new ProfileDTO
            {
                UserId = profile.UserId,
                DisplayName = profile.DisplayName,
                Bio = profile.Bio,
                AvatarUrl = profile.AvatarUrl,
                School = profile.School,
                Department = profile.Department,
                AcademicLevel = profile.AcademicLevel,
                InstructorType = profile.InstructorType,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };
        }
    }
}
