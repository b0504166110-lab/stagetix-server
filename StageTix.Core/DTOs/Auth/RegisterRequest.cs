using System.ComponentModel.DataAnnotations;

namespace StageTix.Core.DTOs.Auth;

/// <summary>
/// Sign-up data. There is intentionally no Role field: every new user is a Customer,
/// so nobody can register as an admin.
/// </summary>
public class RegisterRequest
{
    /// <example>Dana Levi</example>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    /// <example>dana@example.com</example>
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <summary>At least 8 characters, with at least one letter and one digit.</summary>
    /// <example>Passw0rd!</example>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one letter and one digit.")]
    public string Password { get; set; } = string.Empty;
}
