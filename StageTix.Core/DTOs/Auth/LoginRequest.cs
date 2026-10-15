using System.ComponentModel.DataAnnotations;

namespace StageTix.Core.DTOs.Auth;

/// <summary>Credentials for getting a JWT.</summary>
public class LoginRequest
{
    /// <example>dana@example.com</example>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <example>Passw0rd!</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}
