using StageTix.Core.DTOs.Common;
using StageTix.Core.DTOs.Orders;
using StageTix.Core.Entities;

namespace StageTix.Core.Interfaces.Repositories;

public interface IOrderRepository
{
    /// <summary>
    /// Read-only, with show, user and seats loaded. Paging runs inside the database query.
    /// </summary>
    /// <param name="query">Filters and paging.</param>
    /// <param name="userId">When set, only this user's orders are returned.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<PagedResult<Order>> GetPagedAsync(OrderQuery query, int? userId, CancellationToken cancellationToken);

    /// <summary>Read-only, with show, user and seats loaded.</summary>
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Tracked, with seats loaded, for cancelling.</summary>
    Task<Order?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    /// <summary>Tracked, with seats loaded: every active order of a show, for cancelling the show.</summary>
    Task<IReadOnlyList<Order>> GetActiveByShowForUpdateAsync(int showId, CancellationToken cancellationToken);

    /// <summary>Marks the order for insertion.</summary>
    void Add(Order order);
}
