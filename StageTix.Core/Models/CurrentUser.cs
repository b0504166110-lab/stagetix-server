using StageTix.Core.Enums;

namespace StageTix.Core.Models;

/// <summary>
/// The authenticated caller, taken from the JWT claims by the API layer.
/// Never built from data the client sends in the request body or URL.
/// </summary>
public sealed record CurrentUser(int Id, UserRole Role)
{
    public bool IsAdmin => Role == UserRole.Admin;
}
