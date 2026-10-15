using System.ComponentModel.DataAnnotations;
using StageTix.Core.Enums;

namespace StageTix.Core.DTOs.Shows;

/// <summary>
/// Data for creating or updating a show. Status is not here on purpose:
/// it changes only through PATCH /api/shows/{id}/status.
/// </summary>
public class ShowRequest
{
    /// <example>Songs of the Land of Israel</example>
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    /// <example>The Galilee Choir</example>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Artist { get; set; } = string.Empty;

    /// <example>An evening of beloved Israeli songs.</example>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>Start time, ISO 8601 with time zone, e.g. 2026-12-20T18:00:00Z. Must be in the future.</summary>
    public required DateTime StartsAt { get; set; }

    /// <summary>Price of one ticket (1–10,000).</summary>
    /// <example>120</example>
    [Range(typeof(decimal), "1", "10000")]
    public required decimal TicketPrice { get; set; }

    /// <example>Music</example>
    [EnumDataType(typeof(ShowCategory))]
    public required ShowCategory Category { get; set; }
}
