using AutoMapper;
using Microsoft.Extensions.Logging;
using StageTix.Core.DTOs.Auth;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Core.Exceptions;
using StageTix.Core.Interfaces.Repositories;
using StageTix.Core.Interfaces.Security;
using StageTix.Core.Interfaces.Services;
using StageTix.Core.Models;

namespace StageTix.Service.Services;

public class AuthService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordService passwordService,
    IJwtTokenGenerator tokenGenerator,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("This email is already registered.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = passwordService.Hash(request.Password),
            Role = UserRole.Customer,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        userRepository.Add(user);

        // If two sign-ups with the same email race past the check above, the unique index on
        // Email rejects the second one and the UnitOfWork turns it into a ConflictException (409).
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Never log the email, the password or the request body.
        logger.LogInformation("New customer registered: user {UserId}", user.Id);

        return mapper.Map<UserResponse>(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);

        // Same error for "no such user" and "wrong password", so the API does not reveal which emails exist.
        if (user is null || !passwordService.Verify(user.PasswordHash, request.Password))
        {
            logger.LogWarning("Failed login attempt");
            throw new AuthenticationFailedException();
        }

        var token = tokenGenerator.Generate(user);

        logger.LogInformation("User {UserId} logged in as {Role}", user.Id, user.Role);

        return new LoginResponse
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = mapper.Map<UserResponse>(user)
        };
    }

    public async Task<UserResponse> GetUserAsync(int userId, CurrentUser caller, CancellationToken cancellationToken)
    {
        if (!caller.IsAdmin && userId != caller.Id)
        {
            throw new NotFoundException("User", userId);
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new NotFoundException("User", userId);

        return mapper.Map<UserResponse>(user);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
