using StageTix.Core.Enums;

namespace StageTix.Core.DTOs.Shows;

/// <summary>A show as returned by the API.</summary>
public class ShowResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>UTC.</summary>
    public DateTime StartsAt { get; set; }

    public decimal TicketPrice { get; set; }

    public ShowCategory Category { get; set; }

    public ShowStatus Status { get; set; }

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }
}
