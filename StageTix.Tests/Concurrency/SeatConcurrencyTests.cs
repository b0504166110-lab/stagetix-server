using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StageTix.Core.DTOs.Orders;
using StageTix.Core.Enums;
using StageTix.Core.Exceptions;
using StageTix.Core.Models;
using StageTix.Data;
using StageTix.Data.Repositories;
using StageTix.Service.Services;
using StageTix.Tests.TestHelpers;

namespace StageTix.Tests.Concurrency;

/// <summary>
/// Optimistic concurrency on ShowSeat.Version, against a real PostgreSQL database.
/// </summary>
[Collection(PostgresCollection.Name)]
public class SeatConcurrencyTests(PostgresDatabaseFixture database)
{
    private async Task<int> FirstSeatIdAsync(int showId)
    {
        await using var db = database.CreateContext();
        return await db.ShowSeats.Where(s => s.ShowId == showId).OrderBy(s => s.Id).Select(s => s.Id).FirstAsync();
    }

    [SkippableFact]
    public async Task Two_DbContexts_change_the_same_seat_the_first_save_wins_and_the_second_throws()
    {
        Skip.IfNot(database.IsAvailable, database.SkipReason);
        var seatId = await FirstSeatIdAsync(await database.CreateShowAsync());

        // Two separate contexts = two customers. Both read the seat while it is still Available.
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var seatInFirst = await first.ShowSeats.SingleAsync(s => s.Id == seatId);
        var seatInSecond = await second.ShowSeats.SingleAsync(s => s.Id == seatId);
        Assert.Equal(SeatStatus.Available, seatInFirst.Status);
        Assert.Equal(SeatStatus.Available, seatInSecond.Status);
        Assert.Equal(seatInFirst.Version, seatInSecond.Version);

        // Both sell it.
        seatInFirst.Status = SeatStatus.Sold;
        seatInSecond.Status = SeatStatus.Sold;

        // The first save succeeds and replaces the Version.
        await first.SaveChangesAsync();

        // The second save still sends the OLD Version in its WHERE clause → 0 rows → exception.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        // The database holds exactly the first save.
        await using var check = database.CreateContext();
        var stored = await check.ShowSeats.AsNoTracking().SingleAsync(s => s.Id == seatId);
        Assert.Equal(SeatStatus.Sold, stored.Status);
        Assert.Equal(seatInFirst.Version, stored.Version);
    }

    [SkippableFact]
    public async Task The_unit_of_work_turns_the_EF_conflict_into_a_ConcurrencyConflictException()
    {
        Skip.IfNot(database.IsAvailable, database.SkipReason);
        var seatId = await FirstSeatIdAsync(await database.CreateShowAsync());

        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        (await first.ShowSeats.SingleAsync(s => s.Id == seatId)).Status = SeatStatus.Sold;
        (await second.ShowSeats.SingleAsync(s => s.Id == seatId)).Status = SeatStatus.Sold;

        await new UnitOfWork(first).SaveChangesAsync(CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            new UnitOfWork(second).SaveChangesAsync(CancellationToken.None));
        Assert.IsType<DbUpdateConcurrencyException>(ex.InnerException);
    }

    [SkippableFact]
    public async Task Parallel_bookings_of_the_same_seats_through_OrderService_produce_exactly_one_order()
    {
        Skip.IfNot(database.IsAvailable, database.SkipReason);
        var showId = await database.CreateShowAsync(rows: 2, seatsPerRow: 10);
        var dana = new CurrentUser(await database.CreateCustomerAsync(), UserRole.Customer);
        var noa = new CurrentUser(await database.CreateCustomerAsync(), UserRole.Customer);

        List<int> seatIds;
        await using (var db = database.CreateContext())
        {
            seatIds = await db.ShowSeats.Where(s => s.ShowId == showId).OrderBy(s => s.Id).Select(s => s.Id).ToListAsync();
        }

        // 10 rounds; in each, two customers (each with her own DbContext) race for the same pair of seats.
        for (var round = 0; round < 10; round++)
        {
            var request = new CreateOrderRequest { ShowId = showId, SeatIds = [seatIds[round * 2], seatIds[round * 2 + 1]] };

            var outcomes = await Task.WhenAll(
                Task.Run(() => TryBookAsync(request, dana)),
                Task.Run(() => TryBookAsync(request, noa)));

            Assert.Single(outcomes, o => o == "booked");
            Assert.Single(outcomes, o => o == "conflict");
        }

        // No seat is part of two active orders.
        await using var verify = database.CreateContext();
        var seatsInActiveOrders = await verify.Orders
            .Where(o => o.ShowId == showId && o.Status == OrderStatus.Active)
            .SelectMany(o => o.Seats.Select(s => s.Id))
            .ToListAsync();
        Assert.Equal(20, seatsInActiveOrders.Count);
        Assert.Equal(seatsInActiveOrders.Count, seatsInActiveOrders.Distinct().Count());
    }

    /// <summary>A real OrderService with real repositories over its own DbContext — like one HTTP request.</summary>
    private async Task<string> TryBookAsync(CreateOrderRequest request, CurrentUser caller)
    {
        await using var db = database.CreateContext();
        var service = new OrderService(
            new OrderRepository(db), new ShowRepository(db), new ShowSeatRepository(db), new UnitOfWork(db),
            TestMapper.Instance, TimeProvider.System, NullLogger<OrderService>.Instance);

        try
        {
            await service.CreateAsync(request, caller, CancellationToken.None);
            return "booked";
        }
        catch (ConflictException)
        {
            // Either "already taken" (the other order committed before our read)
            // or ConcurrencyConflictException (both read, the other saved first). Both are 409.
            return "conflict";
        }
    }
}
