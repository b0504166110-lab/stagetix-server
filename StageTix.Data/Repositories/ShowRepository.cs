using Microsoft.EntityFrameworkCore;
using StageTix.Core.DTOs.Common;
using StageTix.Core.DTOs.Shows;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Core.Interfaces.Repositories;

namespace StageTix.Data.Repositories;

public class ShowRepository(AppDbContext db) : IShowRepository
{
    public async Task<PagedResult<Show>> GetPagedAsync(ShowQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Show> shows = db.Shows.AsNoTracking();

        if (query.Status is { } status)
        {
            shows = shows.Where(s => s.Status == status);
        }

        if (query.Category is { } category)
        {
            shows = shows.Where(s => s.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ILIKE = case-insensitive LIKE in PostgreSQL. % and _ typed by the user are escaped
            // so they are searched literally instead of acting as wildcards.
            var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
            shows = shows.Where(s => EF.Functions.ILike(s.Title, pattern) || EF.Functions.ILike(s.Artist, pattern));
        }

        IOrderedQueryable<Show> ordered = (query.SortBy, query.SortDirection) switch
        {
            (ShowSortBy.Price, SortDirection.Asc) => shows.OrderBy(s => s.TicketPrice).ThenBy(s => s.StartsAt),
            (ShowSortBy.Price, SortDirection.Desc) => shows.OrderByDescending(s => s.TicketPrice).ThenBy(s => s.StartsAt),
            (_, SortDirection.Desc) => shows.OrderByDescending(s => s.StartsAt),
            _ => shows.OrderBy(s => s.StartsAt)
        };

        // Id as the last sort key keeps the order stable, so no show appears on two pages.
        shows = ordered.ThenBy(s => s.Id);

        var totalCount = await shows.CountAsync(cancellationToken);

        // Skip/Take are translated to OFFSET/LIMIT: only one page is read from the database.
        var items = await shows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Show>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public Task<Show?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        db.Shows.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Show?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        db.Shows.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Show?> GetWithActiveOrdersForUpdateAsync(int id, CancellationToken cancellationToken) =>
        db.Shows
            .Include(s => s.Orders.Where(o => o.Status == OrderStatus.Active))
                .ThenInclude(o => o.Seats)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> HasOrdersAsync(int showId, CancellationToken cancellationToken) =>
        db.Orders.AnyAsync(o => o.ShowId == showId, cancellationToken);

    public void Add(Show show) => db.Shows.Add(show);

    public void Remove(Show show) => db.Shows.Remove(show);

    private static string EscapeLikePattern(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
