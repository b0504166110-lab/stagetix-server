using Microsoft.EntityFrameworkCore;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Core.Interfaces.Repositories;

namespace StageTix.Data.Repositories;

public class ShowSeatRepository(AppDbContext db) : IShowSeatRepository
{
    public async Task<IReadOnlyList<ShowSeat>> GetByShowAsync(int showId, CancellationToken cancellationToken) =>
        await db.ShowSeats
            .AsNoTracking()
            .Where(s => s.ShowId == showId)
            .OrderBy(s => s.Row)
            .ThenBy(s => s.Number)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShowSeat>> GetForUpdateAsync(
        int showId, IReadOnlyCollection<int> seatIds, CancellationToken cancellationToken) =>
        await db.ShowSeats
            .Where(s => s.ShowId == showId && seatIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, SeatCounts>> GetCountsAsync(
        IReadOnlyCollection<int> showIds, CancellationToken cancellationToken)
    {
        if (showIds.Count == 0)
        {
            return new Dictionary<int, SeatCounts>();
        }

        // One GROUP BY query for all the shows on the page, instead of one query per show (N+1).
        var counts = await db.ShowSeats
            .AsNoTracking()
            .Where(s => showIds.Contains(s.ShowId))
            .GroupBy(s => s.ShowId)
            .Select(g => new
            {
                ShowId = g.Key,
                Total = g.Count(),
                Available = g.Count(s => s.Status == SeatStatus.Available)
            })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(c => c.ShowId, c => new SeatCounts(c.Total, c.Available));
    }
}
