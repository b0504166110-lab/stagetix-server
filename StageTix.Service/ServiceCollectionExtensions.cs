using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StageTix.Core.Interfaces.Security;
using StageTix.Core.Interfaces.Services;
using StageTix.Core.Options;
using StageTix.Service.Security;
using StageTix.Service.Services;

namespace StageTix.Service;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the business services and their options.</summary>
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<HallOptions>()
            .Bind(configuration.GetSection(HallOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // The clock is injected (instead of calling DateTime.UtcNow) so tests can control "now".
        services.AddSingleton(TimeProvider.System);

        // Stateless and thread-safe, so one instance is shared by all requests.
        services.AddSingleton<IPasswordService, PasswordService>();

        // Finds every Profile class in this project (ShowProfile, OrderProfile, UserProfile).
        services.AddAutoMapper(typeof(ServiceCollectionExtensions).Assembly);

        // Scoped: they use repositories, which share the request's DbContext.
        // (A Singleton must never hold a Scoped dependency — it would keep one DbContext forever,
        // shared by all requests and threads.)
        services.AddScoped<IShowService, ShowService>();

        return services;
    }
}
