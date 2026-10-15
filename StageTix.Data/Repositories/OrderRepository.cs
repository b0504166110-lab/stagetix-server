using Microsoft.EntityFrameworkCore;
using StageTix.Core.DTOs.Common;
using StageTix.Core.DTOs.Orders;
using StageTix.Core.Entities;
using StageTix.Core.Interfaces.Repositories;

namespace StageTix.Data.Repositories;

public class OrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task<PagedResult<Order>> GetPagedAsync(OrderQuery query, int? userId, CancellationToken cancellationToken)
    {
        IQueryable<Order> orders = db.Orders.AsNoTracking();

        if (userId is { } ownerId)
        {
            orders = orders.Where(o => o.UserId == ownerId);
        }

        if (query.Status is { } status)
        {
            orders = orders.Where(o => o.Status == status);
        }

        if (query.ShowId is { } showId)
        {
            orders = orders.Where(o => o.ShowId == showId);
        }

        var totalCount = await orders.CountAsync(cancellationToken);

        // Related data is loaded with Include in the same round trip — never in a loop (no N+1).
        var items = await WithDetails(orders)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Order>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        WithDetails(db.Orders.AsNoTracking()).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        db.Orders
            .Include(o => o.Seats)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public void Add(Order order) => db.Orders.Add(order);

    private static IQueryable<Order> WithDetails(IQueryable<Order> orders) =>
        orders
            .Include(o => o.Show)
            .Include(o => o.User)
            .Include(o => o.Seats.OrderBy(s => s.Row).ThenBy(s => s.Number))
            .AsSplitQuery();
}
