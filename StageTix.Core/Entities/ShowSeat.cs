using StageTix.Core.Enums;

namespace StageTix.Core.Entities;

/// <summary>
/// One seat in one show — the limited resource that customers compete for.
/// </summary>
public class ShowSeat
{
    public int Id { get; set; }

    public int ShowId { get; set; }

    public Show Show { get; set; } = null!;

    /// <summary>1-based row number.</summary>
    public int Row { get; set; }

    /// <summary>1-based seat number within the row.</summary>
    public int Number { get; set; }

    public SeatStatus Status { get; set; }

    /// <summary>
    /// Concurrency token. Replaced with a new value on every save of this seat,
    /// so a save based on an outdated read is rejected by the database.
    /// </summary>
    public Guid Version { get; set; }

    /// <summary>Many-to-many: a seat can appear in several orders over time (sold, cancelled, sold again).</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
