using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StageTix.API.Extensions;

public static class SwaggerExtensions
{
    public const string BearerSchemeName = "Bearer";

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "StageTix API",
                Version = "v1",
                Description =
                    "Ticket booking for auditorium shows. Seats are the limited resource: two customers who book " +
                    "the same seat at the same moment get one 201 and one 409.\n\n" +
                    "How to call protected endpoints: POST /api/tokens with a demo user " +
                    "(admin@example.com / Admin123!, dana@example.com / Customer123!), copy the token, " +
                    "click Authorize and paste it."
            });

            // The "Authorize" button: an HTTP Bearer (JWT) scheme. Swagger adds the "Bearer " prefix itself.
            options.AddSecurityDefinition(BearerSchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste only the token (without the word Bearer)."
            });

            // /// comments on controllers (API) and DTOs (Core) become descriptions in Swagger.
            foreach (var project in new[] { "StageTix.API", "StageTix.Core" })
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{project}.xml");
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
            }

            // Registered AFTER the XML comments: filters run in order, and the XML filter replaces the
            // description. This one then adds the permission line on top of it.
            options.OperationFilter<AuthorizeOperationFilter>();
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "StageTix API v1");
            options.DocumentTitle = "StageTix API";
            options.DisplayRequestDuration();
        });
        return app;
    }

    /// <summary>
    /// Marks only the endpoints that need a token with the lock icon, and writes the required role
    /// into the description, so Swagger shows the real permissions of each endpoint.
    /// </summary>
    private sealed class AuthorizeOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

            if (metadata.OfType<IAllowAnonymous>().Any())
            {
                operation.Description = Prefix("🔓 Public — no token needed.", operation.Description);
                return;
            }

            var roles = metadata.OfType<IAuthorizeData>()
                .Select(a => a.Roles)
                .Where(r => !string.IsNullOrEmpty(r))
                .ToList();

            var who = roles.Count > 0 ? $"role: {string.Join(", ", roles)}" : "any logged-in user";
            operation.Description = Prefix($"🔒 Requires a token ({who}).", operation.Description);

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerSchemeName, context.Document)] = []
            });
        }

        private static string Prefix(string line, string? description) =>
            string.IsNullOrWhiteSpace(description) ? line : $"{line}\n\n{description}";
    }
}
