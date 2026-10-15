using StageTix.Core.DTOs.Common;
using StageTix.Core.DTOs.Shows;
using StageTix.Core.Entities;

namespace StageTix.Core.Interfaces.Repositories;

public interface IShowRepository
{
    /// <summary>
    /// Read-only. Filtering, sorting and paging (Skip/Take) all run inside the database query.
    /// </summary>
    Task<PagedResult<Show>> GetPagedAsync(ShowQuery query, CancellationToken cancellationToken);

    /// <summary>Read-only.</summary>
    Task<Show?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Tracked, for updates.</summary>
    Task<Show?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Tracked, for cancelling a show: the show with its active orders and each order's seats.
    /// </summary>
    Task<Show?> GetWithActiveOrdersForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<bool> HasOrdersAsync(int showId, CancellationToken cancellationToken);

    /// <summary>Marks the show (and its seats) for insertion.</summary>
    void Add(Show show);

    /// <summary>Marks the show (and its seats) for deletion.</summary>
    void Remove(Show show);
}
