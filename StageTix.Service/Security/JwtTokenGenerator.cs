using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StageTix.Core.Entities;
using StageTix.Core.Interfaces.Security;
using StageTix.Core.Models;
using StageTix.Service.Options;

namespace StageTix.Service.Security;

public class JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtTokenGenerator
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken Generate(User user)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            // Only what the server needs to identify the caller. Never the password or its hash.
            Claims = new Dictionary<string, object>
            {
                [AppClaimTypes.UserId] = user.Id.ToString(),
                [AppClaimTypes.Name] = user.FullName,
                [AppClaimTypes.Role] = user.Role.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
            }
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }
}
