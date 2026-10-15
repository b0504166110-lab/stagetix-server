namespace StageTix.API.Extensions;

public static class CorsExtensions
{
    public const string ClientPolicy = "Client";

    /// <summary>
    /// Allows the React client's origin(s) to call the API from the browser.
    /// Configured in "Cors:AllowedOrigins" — in the cloud: Cors__AllowedOrigins__0=https://your-client.onrender.com
    /// </summary>
    public static IServiceCollection AddClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(ClientPolicy, policy => policy
            .WithOrigins(origins.Select(o => o.TrimEnd('/')).ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Let the browser's JavaScript read these response headers.
            .WithExposedHeaders("Location", Middleware.CorrelationIdMiddleware.HeaderName)));

        return services;
    }
}
