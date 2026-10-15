using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StageTix.API.Errors;
using StageTix.Service.Options;
using StageTix.Service.Security;

namespace StageTix.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured from IOptions<JwtOptions> (lazily), so the key is read from User Secrets /
        // environment variables at runtime and never appears in code or in appsettings.json.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Keep claim names exactly as written in the token ("sub", "role") instead of
                // mapping them to long Microsoft URIs.
                bearer.MapInboundClaims = false;

                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = AppClaimTypes.Name,
                    RoleClaimType = AppClaimTypes.Role
                };

                // Same JSON error shape as the rest of the API.
                bearer.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return ErrorResponses.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                            "Unauthorized", "A valid access token is required. Log in and send it as 'Authorization: Bearer <token>'.");
                    },
                    OnForbidden = context => ErrorResponses.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden,
                        "Forbidden", "You do not have permission to perform this action.")
                };
            });

        services.AddAuthorization(options =>
        {
            // Secure by default: every endpoint requires a valid token unless it is marked [AllowAnonymous].
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
