using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StageTix.Core.Interfaces.Security;
using StageTix.Core.Options;
using StageTix.Service.Security;

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

        return services;
    }
}
