using System.ComponentModel.DataAnnotations;

namespace StageTix.Core.DTOs.Orders;

/// <summary>
/// Book seats in a show. There is no UserId field: the buyer is always the user from the JWT.
/// </summary>
public class CreateOrderRequest
{
    public const int MaxSeatsPerOrder = 6;

    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public required int ShowId { get; set; }

    /// <summary>Ids of the seats to book (1–6), taken from GET /api/shows/{id}/seats.</summary>
    /// <example>[101, 102]</example>
    [MinLength(1)]
    [MaxLength(MaxSeatsPerOrder)]
    public required List<int> SeatIds { get; set; }
}
