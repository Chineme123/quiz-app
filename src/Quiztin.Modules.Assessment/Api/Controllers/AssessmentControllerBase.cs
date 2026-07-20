using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace Quiztin.Modules.Assessment.Api.Controllers
{
    /// <summary>
    /// Shared base for the module's controllers. It exists for one reason: the canonical
    /// identity read below was duplicated byte for byte in every controller, and a third copy
    /// was about to be added with the classroom slice (spec 0008).
    /// </summary>
    public abstract class AssessmentControllerBase : ControllerBase
    {
        // Canonical identity: the Guid from the JWT NameIdentifier claim. No fallback —
        // [Authorize] guarantees an authenticated principal; a missing/invalid id is a 403.
        // One identity for every caller: the same claim answers "which teacher" for authoring
        // and "which student" for the available list, so it is named for neither (§4).
        protected Guid GetCurrentUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(id, out var userId))
                throw new UnauthorizedAccessException("No valid user identity in the token.");
            return userId;
        }
    }
}
