using StageTix.Core.Entities;

namespace StageTix.Core.Interfaces.Repositories;

/// <summary>Access to seats — the limited resource.</summary>
public interface IShowSeatRepository
{
    /// <summary>Read-only. Ordered by row, then number.</summary>
    Task<IReadOnlyList<ShowSeat>> GetByShowAsync(int showId, CancellationToken cancellationToken);

    /// <summary>
    /// Tracked, for booking. Returns only seats that belong to the given show,
    /// so the caller can detect ids from another show.
    /// </summary>
    Task<IReadOnlyList<ShowSeat>> GetForUpdateAsync(int showId, IReadOnlyCollection<int> seatIds, CancellationToken cancellationToken);

    /// <summary>
    /// Total and available seat counts for several shows in ONE query (avoids N+1).
    /// Shows without seats are missing from the result.
    /// </summary>
    Task<IReadOnlyDictionary<int, SeatCounts>> GetCountsAsync(IReadOnlyCollection<int> showIds, CancellationToken cancellationToken);
}

/// <summary>Seat counts of one show.</summary>
public sealed record SeatCounts(int Total, int Available);
