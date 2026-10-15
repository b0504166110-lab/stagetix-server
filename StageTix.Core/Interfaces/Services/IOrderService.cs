using StageTix.Core.DTOs.Common;
using StageTix.Core.DTOs.Orders;
using StageTix.Core.Models;

namespace StageTix.Core.Interfaces.Services;

public interface IOrderService
{
    /// <summary>
    /// Books the requested seats for the caller — all of them or none.
    /// </summary>
    /// <exception cref="Exceptions.ConflictException">
    /// A seat is already sold, or was sold by someone else at the same moment
    /// (<see cref="Exceptions.ConcurrencyConflictException"/>).
    /// </exception>
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CurrentUser caller, CancellationToken cancellationToken);

    /// <summary>A customer gets only her own orders; an admin gets all orders.</summary>
    Task<PagedResult<OrderResponse>> GetOrdersAsync(OrderQuery query, CurrentUser caller, CancellationToken cancellationToken);

    /// <exception cref="Exceptions.NotFoundException">Missing, or belongs to another customer.</exception>
    Task<OrderResponse> GetOrderAsync(int id, CurrentUser caller, CancellationToken cancellationToken);

    /// <summary>Admin only: cancels the order and releases its seats.</summary>
    Task ChangeStatusAsync(int id, OrderStatusRequest request, CancellationToken cancellationToken);
}
