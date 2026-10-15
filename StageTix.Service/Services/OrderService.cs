using AutoMapper;
using Microsoft.Extensions.Logging;
using StageTix.Core.DTOs.Common;
using StageTix.Core.DTOs.Orders;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Core.Exceptions;
using StageTix.Core.Interfaces.Repositories;
using StageTix.Core.Interfaces.Services;
using StageTix.Core.Models;

namespace StageTix.Service.Services;

public class OrderService(
    IOrderRepository orderRepository,
    IShowRepository showRepository,
    IShowSeatRepository seatRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<OrderService> logger) : IOrderService
{
    public const string SeatTakenMessage =
        "One or more of the selected seats was just taken by someone else. Please choose other seats.";

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CurrentUser caller, CancellationToken cancellationToken)
    {
        var seatIds = request.SeatIds;

        if (seatIds.Count is 0 or > CreateOrderRequest.MaxSeatsPerOrder)
        {
            throw new BusinessRuleException($"An order must contain between 1 and {CreateOrderRequest.MaxSeatsPerOrder} seats.");
        }

        if (seatIds.Distinct().Count() != seatIds.Count)
        {
            throw new BusinessRuleException("The same seat was selected more than once.");
        }

        int orderId;
        try
        {
            // The availability check and the save run in ONE transaction. Even so, the check alone
            // is not enough: another request can sell the seat right after we read it. The seat's
            // Version concurrency token closes that gap at save time.
            orderId = await unitOfWork.ExecuteInTransactionAsync(
                ct => BookSeatsAsync(request.ShowId, seatIds, caller.Id, ct), cancellationToken);
        }
        catch (ConcurrencyConflictException ex)
        {
            logger.LogWarning(
                "Seat conflict: user {UserId} lost the race for seats {SeatIds} in show {ShowId} (concurrency token mismatch). Order rejected",
                caller.Id, seatIds, request.ShowId);

            throw new ConcurrencyConflictException(SeatTakenMessage, ex);
        }

        logger.LogInformation("Order {OrderId} created by user {UserId}: show {ShowId}, {SeatCount} seat(s)",
            orderId, caller.Id, request.ShowId, seatIds.Count);

        var created = await orderRepository.GetByIdAsync(orderId, cancellationToken)
                      ?? throw new NotFoundException("Order", orderId);
        return mapper.Map<OrderResponse>(created);
    }

    public async Task<PagedResult<OrderResponse>> GetOrdersAsync(OrderQuery query, CurrentUser caller, CancellationToken cancellationToken)
    {
        // A customer always gets only her own orders; the admin gets everyone's.
        int? ownerFilter = caller.IsAdmin ? null : caller.Id;

        var page = await orderRepository.GetPagedAsync(query, ownerFilter, cancellationToken);

        return new PagedResult<OrderResponse>
        {
            Items = mapper.Map<List<OrderResponse>>(page.Items),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<OrderResponse> GetOrderAsync(int id, CurrentUser caller, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken);

        // Another customer's order is reported as "not found", so ids of other people's orders leak nothing.
        if (order is null || (!caller.IsAdmin && order.UserId != caller.Id))
        {
            throw new NotFoundException("Order", id);
        }

        return mapper.Map<OrderResponse>(order);
    }

    public async Task ChangeStatusAsync(int id, OrderStatusRequest request, CancellationToken cancellationToken)
    {
        if (request.Status != OrderStatus.Cancelled)
        {
            throw new BusinessRuleException("An order can only be changed to Cancelled.");
        }

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await orderRepository.GetForUpdateAsync(id, ct)
                        ?? throw new NotFoundException("Order", id);

            if (order.Status == OrderStatus.Cancelled)
            {
                throw new ConflictException("The order is already cancelled.");
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = Now;

            // Released seats get a new Version on save, so a customer who read them as Sold
            // and anyone racing on them is still protected by the concurrency token.
            foreach (var seat in order.Seats)
            {
                seat.Status = SeatStatus.Available;
            }

            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation("Order {OrderId} cancelled by admin; {SeatCount} seat(s) released", id, order.Seats.Count);
        }, cancellationToken);
    }

    /// <summary>Runs inside the transaction. Returns the new order id.</summary>
    private async Task<int> BookSeatsAsync(int showId, List<int> seatIds, int userId, CancellationToken cancellationToken)
    {
        var show = await showRepository.GetByIdAsync(showId, cancellationToken);

        // A draft is invisible to customers, so it is reported as not found.
        if (show is null || show.Status == ShowStatus.Draft)
        {
            throw new NotFoundException("Show", showId);
        }

        if (show.Status != ShowStatus.OnSale)
        {
            throw new ConflictException("This show is not on sale.");
        }

        if (show.StartsAt <= Now)
        {
            throw new BusinessRuleException("This show has already started.");
        }

        var seats = await seatRepository.GetForUpdateAsync(showId, seatIds, cancellationToken);

        if (seats.Count != seatIds.Count)
        {
            var unknown = seatIds.Except(seats.Select(s => s.Id));
            throw new BusinessRuleException($"Seat(s) {string.Join(", ", unknown)} do not belong to show {showId}.");
        }

        var alreadySold = seats.Where(s => s.Status != SeatStatus.Available).ToList();
        if (alreadySold.Count > 0)
        {
            var names = string.Join(", ", alreadySold.OrderBy(s => s.Row).ThenBy(s => s.Number).Select(s => $"row {s.Row} seat {s.Number}"));

            logger.LogWarning("Seat conflict: user {UserId} requested already sold seat(s) {Seats} in show {ShowId}. Order rejected",
                userId, names, showId);

            throw new ConflictException($"These seats are already taken: {names}. Please choose other seats.");
        }

        foreach (var seat in seats)
        {
            seat.Status = SeatStatus.Sold;
        }

        var order = new Order
        {
            UserId = userId,
            ShowId = showId,
            Seats = seats.ToList(),
            TotalPrice = show.TicketPrice * seats.Count,
            Status = OrderStatus.Active,
            CreatedAt = Now
        };
        orderRepository.Add(order);

        // UPDATE "ShowSeats" ... WHERE "Id" = @id AND "Version" = @versionWeRead
        // → 0 rows if someone else saved the seat first → ConcurrencyConflictException.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.Id;
    }

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;
}
