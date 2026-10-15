using Microsoft.AspNetCore.Identity;
using StageTix.Core.Entities;
using StageTix.Core.Interfaces.Security;

namespace StageTix.Service.Security;

/// <summary>
/// Uses ASP.NET Core Identity's PasswordHasher: PBKDF2 with HMAC-SHA512, a random salt per
/// password and many iterations. The salt and settings are stored inside the hash string.
/// </summary>
public class PasswordService : IPasswordService
{
    private static readonly PasswordHasher<User> Hasher = new();

    // The default hasher ignores the user argument, so no user instance is needed.
    public string Hash(string password) => Hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        Hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
