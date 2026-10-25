using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Core.Models;

namespace StageTix.Tests.TestHelpers;

/// <summary>Builders for the entities and callers used across the tests.</summary>
public static class TestData
{
    public static readonly CurrentUser Admin = new(1, UserRole.Admin);
    public static readonly CurrentUser Dana = new(2, UserRole.Customer);
    public static readonly CurrentUser Noa = new(3, UserRole.Customer);

    public static User UserOf(CurrentUser caller, string passwordHash = "hash") => new()
    {
        Id = caller.Id,
        FullName = $"User {caller.Id}",
        Email = $"user{caller.Id}@example.com",
        PasswordHash = passwordHash,
        Role = caller.Role
    };

    /// <summary>A show with seat ids 1..rows*seatsPerRow, all available, starting 10 days after the fixed clock.</summary>
    public static Show Show(
        int id = 1,
        ShowStatus status = ShowStatus.OnSale,
        DateTime? startsAt = null,
        decimal price = 100,
        int rows = 2,
        int seatsPerRow = 5)
    {
        var show = new Show
        {
            Id = id,
            Title = $"Show {id}",
            Artist = "Test Artist",
            StartsAt = startsAt ?? FixedTimeProvider.DefaultNow.AddDays(10),
            TicketPrice = price,
            Category = ShowCategory.Charity,
            Status = status
        };
        show.GenerateSeats(rows, seatsPerRow);

        var seatId = 1;
        foreach (var seat in show.Seats)
        {
            seat.Id = seatId++;
            seat.ShowId = id;
            seat.Show = show;
            seat.Version = Guid.NewGuid();
        }

        return show;
    }

    /// <summary>The seats of <paramref name="show"/> with the given ids, as the seat repository would return them.</summary>
    public static List<ShowSeat> Seats(Show show, params int[] ids) =>
        show.Seats.Where(s => ids.Contains(s.Id)).ToList();
}
