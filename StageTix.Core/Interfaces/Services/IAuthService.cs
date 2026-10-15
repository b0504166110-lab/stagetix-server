using StageTix.Core.DTOs.Auth;

namespace StageTix.Core.Interfaces.Services;

public interface IAuthService
{
    /// <summary>Creates a new Customer.</summary>
    /// <exception cref="Exceptions.ConflictException">The email is already registered.</exception>
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    /// <summary>Checks the credentials and issues a JWT.</summary>
    /// <exception cref="Exceptions.AuthenticationFailedException">Wrong email or password.</exception>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <exception cref="Exceptions.NotFoundException">The user from the token no longer exists.</exception>
    Task<UserResponse> GetUserAsync(int userId, CancellationToken cancellationToken);
}
