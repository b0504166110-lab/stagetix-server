using System.Security.Claims;
using StageTix.Core.Enums;
using StageTix.Core.Models;
using StageTix.Service.Security;

namespace StageTix.API.Security;

/// <summary>
/// Reads the caller's identity from the validated JWT claims.
/// This is the ONLY source of "who is calling" — never an id sent in the URL or body.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>For anonymous-allowed endpoints: null when there is no valid token.</summary>
    public static CurrentUser? FindCurrentUser(this ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var id = principal.FindFirstValue(AppClaimTypes.UserId);
        var role = principal.FindFirstValue(AppClaimTypes.Role);

        return int.TryParse(id, out var userId) && Enum.TryParse<UserRole>(role, out var userRole)
            ? new CurrentUser(userId, userRole)
            : null;
    }

    /// <summary>For [Authorize] endpoints, where the authentication middleware already guaranteed a valid token.</summary>
    public static CurrentUser GetCurrentUser(this ClaimsPrincipal principal) =>
        principal.FindCurrentUser()
        ?? throw new InvalidOperationException("GetCurrentUser was called on an endpoint without [Authorize].");
}
