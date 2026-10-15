using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StageTix.Core.DTOs.Auth;
using StageTix.Core.DTOs.Common;
using StageTix.Core.Interfaces.Services;

namespace StageTix.API.Controllers;

/// <summary>Login: exchanging credentials for an access token (a "token" resource is created).</summary>
[ApiController]
[Route("api/tokens")]
[Produces("application/json")]
public class TokensController(IAuthService authService) : ControllerBase
{
    /// <summary>Log in and get a JWT.</summary>
    /// <remarks>
    /// Public. Copy the returned token into Swagger's Authorize button, or send it as
    /// "Authorization: Bearer &lt;token&gt;". The token expires after the time in expiresAt.
    /// </remarks>
    /// <response code="200">Logged in. The response contains the token and the user.</response>
    /// <response code="400">Missing or invalid fields.</response>
    /// <response code="401">Wrong email or password.</response>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await authService.LoginAsync(request, cancellationToken));
    }
}
