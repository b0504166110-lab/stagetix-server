using StageTix.Core.Enums;

namespace StageTix.Core.DTOs.Orders;

/// <summary>An order as returned by the API.</summary>
public class OrderResponse
{
    public int Id { get; set; }

    public int ShowId { get; set; }

    public string ShowTitle { get; set; } = string.Empty;

    /// <summary>UTC.</summary>
    public DateTime ShowStartsAt { get; set; }

    public int UserId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public decimal TotalPrice { get; set; }

    public OrderStatus Status { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC. Null while the order is active.</summary>
    public DateTime? CancelledAt { get; set; }

    public IReadOnlyList<OrderSeatResponse> Seats { get; set; } = [];
}
