namespace StageTix.Core.DTOs.Auth;

/// <summary>Returned after a successful login.</summary>
public class LoginResponse
{
    /// <summary>Send as "Authorization: Bearer {token}".</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>UTC time after which the token is rejected.</summary>
    public DateTime ExpiresAt { get; set; }

    public UserResponse User { get; set; } = new();
}
