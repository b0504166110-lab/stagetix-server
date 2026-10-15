using StageTix.Core.Enums;

namespace StageTix.Core.Entities;

/// <summary>A registered user: a customer or the hall admin.</summary>
public class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    /// <summary>Unique. Stored in lower case so look-ups are case-insensitive.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Never the plain password. Never returned by the API.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>One-to-many: a user has many orders.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
