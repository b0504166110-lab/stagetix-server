using System.ComponentModel.DataAnnotations;

namespace StageTix.Service.Options;

/// <summary>
/// JWT settings, bound from the "Jwt" configuration section.
/// SigningKey is a secret: User Secrets in development, the Jwt__SigningKey environment variable in the cloud.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC-SHA256 key. At least 32 characters (256 bits).</summary>
    [Required]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters long.")]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>How long a token stays valid.</summary>
    [Range(5, 1440)]
    public int ExpiryMinutes { get; set; } = 60;
}
