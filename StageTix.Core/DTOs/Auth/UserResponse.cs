using StageTix.Core.Enums;

namespace StageTix.Core.DTOs.Auth;

/// <summary>Public user data. Never includes the password hash.</summary>
public class UserResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }
}
