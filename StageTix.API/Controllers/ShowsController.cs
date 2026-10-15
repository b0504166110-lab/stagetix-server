using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StageTix.API.Security;
using StageTix.Core.DTOs.Common;
using StageTix.Core.DTOs.Shows;
using StageTix.Core.Interfaces.Services;

namespace StageTix.API.Controllers;

/// <summary>Shows and their seat maps.</summary>
[ApiController]
[Route("api/shows")]
[Produces("application/json")]
public class ShowsController(IShowService showService) : ControllerBase
{
    /// <summary>List shows with search, filter, sort and paging.</summary>
    /// <remarks>
    /// Public. Guests and customers always get only OnSale shows.
    /// An admin (with a token) can filter by any status, including Draft and Cancelled.
    /// Example: /api/shows?search=jazz&amp;category=Music&amp;sortBy=Price&amp;sortDirection=Desc&amp;page=1&amp;pageSize=10
    /// </remarks>
    /// <response code="200">One page of shows.</response>
    /// <response code="400">Invalid query (e.g. pageSize above 50).</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<ShowResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ShowResponse>>> GetShows([FromQuery] ShowQuery query, CancellationToken cancellationToken)
    {
        return Ok(await showService.GetShowsAsync(query, User.FindCurrentUser(), cancellationToken));
    }

    /// <summary>Get one show.</summary>
    /// <remarks>Public. A draft is visible only to the admin (404 for everyone else).</remarks>
    /// <response code="200">The show, including total and available seat counts.</response>
    /// <response code="404">No such show, or a draft requested by a non-admin.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<ShowResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShowResponse>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        return Ok(await showService.GetShowAsync(id, User.FindCurrentUser(), cancellationToken));
    }

    /// <summary>Get the seat map of a show.</summary>
    /// <remarks>Public. Shows which seats are Available or Sold — never who bought them. Use the seat ids to book.</remarks>
    /// <response code="200">All seats, ordered by row and number.</response>
    /// <response code="404">No such show, or a draft requested by a non-admin.</response>
    [HttpGet("{id:int}/seats")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<SeatResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SeatResponse>>> GetSeats([FromRoute] int id, CancellationToken cancellationToken)
    {
        return Ok(await showService.GetSeatsAsync(id, User.FindCurrentUser(), cancellationToken));
    }

    /// <summary>Create a show (as a Draft), together with all of its seats.</summary>
    /// <remarks>Admin only.</remarks>
    /// <response code="201">Created. The Location header points to the new show.</response>
    /// <response code="400">Invalid fields, or a start time in the past.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is not an admin.</response>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType<ShowResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShowResponse>> Create([FromBody] ShowRequest request, CancellationToken cancellationToken)
    {
        var show = await showService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = show.Id }, show);
    }

    /// <summary>Update a show's details.</summary>
    /// <remarks>Admin only. Status is changed with PATCH /api/shows/{id}/status. Existing orders keep the price they paid.</remarks>
    /// <response code="200">The updated show.</response>
    /// <response code="400">Invalid fields, or a new start time in the past.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is not an admin.</response>
    /// <response code="404">No such show.</response>
    /// <response code="409">The show is cancelled and cannot be edited.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType<ShowResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShowResponse>> Update([FromRoute] int id, [FromBody] ShowRequest request, CancellationToken cancellationToken)
    {
        return Ok(await showService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Publish a draft (OnSale) or cancel a show (Cancelled).</summary>
    /// <remarks>Admin only. Cancelling a show also cancels all of its active orders and releases their seats.</remarks>
    /// <response code="204">Status changed.</response>
    /// <response code="400">Invalid target status (e.g. back to Draft).</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is not an admin.</response>
    /// <response code="404">No such show.</response>
    /// <response code="409">The show is already cancelled.</response>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus([FromRoute] int id, [FromBody] ShowStatusRequest request, CancellationToken cancellationToken)
    {
        await showService.ChangeStatusAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Delete a draft show.</summary>
    /// <remarks>Admin only. Only a Draft without orders can be deleted; a published show must be cancelled instead.</remarks>
    /// <response code="204">Deleted.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is not an admin.</response>
    /// <response code="404">No such show.</response>
    /// <response code="409">The show is not a draft, or it has orders.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        await showService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
