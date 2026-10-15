using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StageTix.API.Security;
using StageTix.Core.DTOs.Auth;
using StageTix.Core.DTOs.Common;
using StageTix.Core.Interfaces.Services;

namespace StageTix.API.Controllers;

/// <summary>Registration and user details.</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UsersController(IAuthService authService) : ControllerBase
{
    /// <summary>Register a new customer.</summary>
    /// <remarks>Public. Every new user gets the Customer role; there is no way to register as an admin.</remarks>
    /// <response code="201">The user was created. Log in with POST /api/tokens to get a token.</response>
    /// <response code="400">Invalid fields (e.g. password shorter than 8 characters).</response>
    /// <response code="409">The email is already registered.</response>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>Get the logged-in user.</summary>
    /// <remarks>Requires a token (any role).</remarks>
    /// <response code="200">The current user.</response>
    /// <response code="401">Missing, invalid or expired token.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
    {
        var caller = User.GetCurrentUser();
        return Ok(await authService.GetUserAsync(caller.Id, caller, cancellationToken));
    }

    /// <summary>Get a user by id.</summary>
    /// <remarks>Requires a token. A customer may read only herself (others return 404); the admin may read anyone.</remarks>
    /// <response code="200">The user.</response>
    /// <response code="401">Missing, invalid or expired token.</response>
    /// <response code="404">No such user, or not visible to the caller.</response>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        return Ok(await authService.GetUserAsync(id, User.GetCurrentUser(), cancellationToken));
    }
}
