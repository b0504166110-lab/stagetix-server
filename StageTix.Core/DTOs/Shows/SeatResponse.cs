using StageTix.Core.Enums;

namespace StageTix.Core.DTOs.Shows;

/// <summary>One seat on the seat map. Does not reveal who bought it.</summary>
public class SeatResponse
{
    public int Id { get; set; }

    public int Row { get; set; }

    public int Number { get; set; }

    public SeatStatus Status { get; set; }
}
