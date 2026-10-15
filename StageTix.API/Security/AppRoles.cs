using StageTix.Core.Enums;

namespace StageTix.API.Security;

/// <summary>Role names for [Authorize(Roles = ...)]. Taken from the enum so they can never drift apart.</summary>
public static class AppRoles
{
    public const string Admin = nameof(UserRole.Admin);
    public const string Customer = nameof(UserRole.Customer);
}
