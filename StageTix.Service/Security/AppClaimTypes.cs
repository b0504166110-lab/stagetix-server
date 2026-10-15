using Microsoft.IdentityModel.JsonWebTokens;

namespace StageTix.Service.Security;

/// <summary>Claim names written into the JWT and read back by the API. One place, so both sides agree.</summary>
public static class AppClaimTypes
{
    public const string UserId = JwtRegisteredClaimNames.Sub;
    public const string Name = JwtRegisteredClaimNames.Name;
    public const string Role = "role";
}
