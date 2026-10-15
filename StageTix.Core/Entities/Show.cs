using StageTix.Core.Enums;

namespace StageTix.Core.Entities;

/// <summary>A single performance in the auditorium.</summary>
public class Show
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>UTC.</summary>
    public DateTime StartsAt { get; set; }

    /// <summary>One price for every seat in the show.</summary>
    public decimal TicketPrice { get; set; }

    public ShowCategory Category { get; set; }

    public ShowStatus Status { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>One-to-many: a show has a seat per hall position, created together with the show.</summary>
    public ICollection<ShowSeat> Seats { get; set; } = new List<ShowSeat>();

    /// <summary>One-to-many: a show has many orders.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>Adds one available seat for every position in the hall.</summary>
    public void GenerateSeats(int rows, int seatsPerRow)
    {
        for (var row = 1; row <= rows; row++)
        {
            for (var number = 1; number <= seatsPerRow; number++)
            {
                Seats.Add(new ShowSeat { Row = row, Number = number, Status = SeatStatus.Available });
            }
        }
    }
}
