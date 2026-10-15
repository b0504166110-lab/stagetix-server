using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StageTix.API.Security;
using StageTix.Core.DTOs.Common;
using StageTix.Core.DTOs.Orders;
using StageTix.Core.Interfaces.Services;

namespace StageTix.API.Controllers;

/// <summary>Seat bookings — the limited resource.</summary>
[ApiController]
[Route("api/orders")]
[Authorize]
[Produces("application/json")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    /// <summary>Book seats in a show (1–6 seats, all or nothing).</summary>
    /// <remarks>
    /// Customer only. The buyer is taken from the token, never from the request.
    /// If any seat is already sold — or another customer buys it at the same moment — the whole order is
    /// rejected with 409 and nothing is booked. Refresh the seat map and choose again.
    /// </remarks>
    /// <response code="201">Booked. The Location header points to the new order.</response>
    /// <response code="400">Invalid request: duplicate seats, seats of another show, more than 6 seats, or the show already started.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is not a customer (an admin cannot buy).</response>
    /// <response code="404">No such show, or the show is a draft.</response>
    /// <response code="409">A seat is already taken (or was just taken by someone else), or the show is not on sale.</response>
    [HttpPost]
    [Authorize(Roles = AppRoles.Customer)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await orderService.CreateAsync(request, User.GetCurrentUser(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>List orders with paging.</summary>
    /// <remarks>Any logged-in user. A customer gets only her own orders; the admin gets everyone's.</remarks>
    /// <response code="200">One page of orders, newest first.</response>
    /// <response code="400">Invalid query.</response>
    /// <response code="401">Missing or invalid token.</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<OrderResponse>>> GetOrders([FromQuery] OrderQuery query, CancellationToken cancellationToken)
    {
        return Ok(await orderService.GetOrdersAsync(query, User.GetCurrentUser(), cancellationToken));
    }

    /// <summary>Get one order.</summary>
    /// <remarks>The owner or the admin. Another customer's order returns 404.</remarks>
    /// <response code="200">The order.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="404">No such order, or it belongs to another customer.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        return Ok(await orderService.GetOrderAsync(id, User.GetCurrentUser(), cancellationToken));
    }

    /// <summary>Cancel an order and release its seats.</summary>
    /// <remarks>Admin only. The only supported status is Cancelled.</remarks>
    /// <response code="204">Cancelled; the seats are available again.</response>
    /// <response code="400">A status other than Cancelled.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is not an admin.</response>
    /// <response code="404">No such order.</response>
    /// <response code="409">The order is already cancelled.</response>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus([FromRoute] int id, [FromBody] OrderStatusRequest request, CancellationToken cancellationToken)
    {
        await orderService.ChangeStatusAsync(id, request, cancellationToken);
        return NoContent();
    }
}
