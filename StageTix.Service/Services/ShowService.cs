using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StageTix.Core.DTOs.Common;
using StageTix.Core.DTOs.Shows;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Core.Exceptions;
using StageTix.Core.Interfaces.Repositories;
using StageTix.Core.Interfaces.Services;
using StageTix.Core.Models;
using StageTix.Core.Options;

namespace StageTix.Service.Services;

public class ShowService(
    IShowRepository showRepository,
    IShowSeatRepository seatRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IOptions<HallOptions> hallOptions,
    TimeProvider timeProvider,
    ILogger<ShowService> logger) : IShowService
{
    private readonly HallOptions _hall = hallOptions.Value;

    public async Task<PagedResult<ShowResponse>> GetShowsAsync(ShowQuery query, CurrentUser? caller, CancellationToken cancellationToken)
    {
        // Only the admin may list drafts and cancelled shows; everyone else always gets OnSale shows,
        // whatever Status they sent.
        if (caller is not { IsAdmin: true })
        {
            query.Status = ShowStatus.OnSale;
        }

        logger.LogDebug(
            "Listing shows: search={Search} category={Category} status={Status} sort={SortBy} {SortDirection} page={Page} size={PageSize}",
            query.Search, query.Category, query.Status, query.SortBy, query.SortDirection, query.Page, query.PageSize);

        var page = await showRepository.GetPagedAsync(query, cancellationToken);

        var showIds = page.Items.Select(s => s.Id).ToList();
        var counts = await seatRepository.GetCountsAsync(showIds, cancellationToken);

        return new PagedResult<ShowResponse>
        {
            Items = page.Items.Select(show => ToResponse(show, counts)).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<ShowResponse> GetShowAsync(int id, CurrentUser? caller, CancellationToken cancellationToken)
    {
        var show = await GetVisibleShowAsync(id, caller, cancellationToken);
        var counts = await seatRepository.GetCountsAsync([show.Id], cancellationToken);
        return ToResponse(show, counts);
    }

    public async Task<IReadOnlyList<SeatResponse>> GetSeatsAsync(int showId, CurrentUser? caller, CancellationToken cancellationToken)
    {
        await GetVisibleShowAsync(showId, caller, cancellationToken);
        var seats = await seatRepository.GetByShowAsync(showId, cancellationToken);
        return mapper.Map<List<SeatResponse>>(seats);
    }

    public async Task<ShowResponse> CreateAsync(ShowRequest request, CancellationToken cancellationToken)
    {
        var startsAt = ToUtc(request.StartsAt);
        EnsureInFuture(startsAt);

        var show = mapper.Map<Show>(request);
        show.StartsAt = startsAt;
        show.Status = ShowStatus.Draft;
        show.CreatedAt = Now;
        show.GenerateSeats(_hall.Rows, _hall.SeatsPerRow);

        showRepository.Add(show);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Show {ShowId} created as Draft with {SeatCount} seats", show.Id, show.Seats.Count);

        var response = mapper.Map<ShowResponse>(show);
        response.TotalSeats = response.AvailableSeats = show.Seats.Count;
        return response;
    }

    public async Task<ShowResponse> UpdateAsync(int id, ShowRequest request, CancellationToken cancellationToken)
    {
        var show = await showRepository.GetForUpdateAsync(id, cancellationToken)
                   ?? throw new NotFoundException("Show", id);

        if (show.Status == ShowStatus.Cancelled)
        {
            throw new ConflictException("A cancelled show cannot be edited.");
        }

        var startsAt = ToUtc(request.StartsAt);
        if (startsAt != show.StartsAt)
        {
            EnsureInFuture(startsAt);
        }

        // Copies the allowed fields onto the tracked entity; existing orders keep the price they paid.
        mapper.Map(request, show);
        show.StartsAt = startsAt;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Show {ShowId} updated", show.Id);

        var counts = await seatRepository.GetCountsAsync([show.Id], cancellationToken);
        return ToResponse(show, counts);
    }

    public async Task ChangeStatusAsync(int id, ShowStatusRequest request, CancellationToken cancellationToken)
    {
        switch (request.Status)
        {
            case ShowStatus.OnSale:
                await PublishAsync(id, cancellationToken);
                break;
            case ShowStatus.Cancelled:
                await CancelAsync(id, cancellationToken);
                break;
            default:
                throw new BusinessRuleException("A show cannot be moved back to Draft.");
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var show = await showRepository.GetForUpdateAsync(id, cancellationToken)
                   ?? throw new NotFoundException("Show", id);

        if (show.Status != ShowStatus.Draft)
        {
            throw new ConflictException("Only a draft can be deleted. Cancel the show instead.");
        }

        if (await showRepository.HasOrdersAsync(id, cancellationToken))
        {
            throw new ConflictException("A show that has orders cannot be deleted.");
        }

        showRepository.Remove(show);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Draft show {ShowId} deleted", id);
    }

    private async Task PublishAsync(int id, CancellationToken cancellationToken)
    {
        var show = await showRepository.GetForUpdateAsync(id, cancellationToken)
                   ?? throw new NotFoundException("Show", id);

        switch (show.Status)
        {
            case ShowStatus.OnSale:
                return; // Already published: nothing to do.
            case ShowStatus.Cancelled:
                throw new ConflictException("A cancelled show cannot be put on sale again.");
        }

        EnsureInFuture(show.StartsAt);

        show.Status = ShowStatus.OnSale;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Show {ShowId} is now on sale", id);
    }

    private Task CancelAsync(int id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var show = await showRepository.GetWithActiveOrdersForUpdateAsync(id, ct)
                       ?? throw new NotFoundException("Show", id);

            if (show.Status == ShowStatus.Cancelled)
            {
                throw new ConflictException("The show is already cancelled.");
            }

            var now = Now;
            show.Status = ShowStatus.Cancelled;

            // Every active order is cancelled and its seats are released, so no seat stays "Sold"
            // without an active order behind it.
            foreach (var order in show.Orders)
            {
                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = now;
                foreach (var seat in order.Seats)
                {
                    seat.Status = SeatStatus.Available;
                }
            }

            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation("Show {ShowId} cancelled together with {OrderCount} active order(s)", id, show.Orders.Count);
        }, cancellationToken);

    /// <summary>Drafts are visible only to the admin; for anyone else they do not exist (404).</summary>
    private async Task<Show> GetVisibleShowAsync(int id, CurrentUser? caller, CancellationToken cancellationToken)
    {
        var show = await showRepository.GetByIdAsync(id, cancellationToken);

        if (show is null || (show.Status == ShowStatus.Draft && caller is not { IsAdmin: true }))
        {
            throw new NotFoundException("Show", id);
        }

        return show;
    }

    private ShowResponse ToResponse(Show show, IReadOnlyDictionary<int, SeatCounts> counts)
    {
        var response = mapper.Map<ShowResponse>(show);
        if (counts.TryGetValue(show.Id, out var c))
        {
            response.TotalSeats = c.Total;
            response.AvailableSeats = c.Available;
        }
        return response;
    }

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    private void EnsureInFuture(DateTime startsAtUtc)
    {
        if (startsAtUtc <= Now)
        {
            throw new BusinessRuleException("The show start time must be in the future.");
        }
    }

    /// <summary>
    /// PostgreSQL "timestamp with time zone" accepts only UTC values. A time sent without a zone
    /// is treated as UTC; a time with an offset is converted to UTC.
    /// </summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
