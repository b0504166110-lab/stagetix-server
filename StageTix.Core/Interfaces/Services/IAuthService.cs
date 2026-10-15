using StageTix.Core.DTOs.Auth;
using StageTix.Core.Models;

namespace StageTix.Core.Interfaces.Services;

public interface IAuthService
{
    /// <summary>Creates a new Customer. Nobody can register as an admin.</summary>
    /// <exception cref="Exceptions.ConflictException">The email is already registered.</exception>
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    /// <summary>Checks the credentials and issues a JWT.</summary>
    /// <exception cref="Exceptions.AuthenticationFailedException">Wrong email or password.</exception>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>A customer may read only herself; the admin may read anyone.</summary>
    /// <exception cref="Exceptions.NotFoundException">Missing, or another user requested by a customer.</exception>
    Task<UserResponse> GetUserAsync(int userId, CurrentUser caller, CancellationToken cancellationToken);
}
