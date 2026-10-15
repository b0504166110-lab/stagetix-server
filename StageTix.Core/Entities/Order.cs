using StageTix.Core.Enums;

namespace StageTix.Core.Entities;

/// <summary>A customer's purchase of one or more seats in one show.</summary>
public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int ShowId { get; set; }

    public Show Show { get; set; } = null!;

    /// <summary>Ticket price at the time of purchase multiplied by the number of seats.</summary>
    public decimal TotalPrice { get; set; }

    public OrderStatus Status { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Many-to-many: the seats bought in this order.</summary>
    public ICollection<ShowSeat> Seats { get; set; } = new List<ShowSeat>();
}
