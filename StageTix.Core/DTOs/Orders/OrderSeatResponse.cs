namespace StageTix.Core.DTOs.Orders;

/// <summary>A seat that belongs to an order.</summary>
public class OrderSeatResponse
{
    public int Id { get; set; }

    public int Row { get; set; }

    public int Number { get; set; }
}
