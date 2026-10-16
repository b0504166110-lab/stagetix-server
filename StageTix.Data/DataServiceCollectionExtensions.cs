using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StageTix.Core.Interfaces.Repositories;
using StageTix.Data.Repositories;
using StageTix.Data.Seed;

namespace StageTix.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Registers the DbContext, the repositories and the unit of work.
    /// All are Scoped: one instance per HTTP request, so every repository in a request
    /// shares the same DbContext and the same transaction.
    /// </summary>
    public static IServiceCollection AddDataLayer(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var npgsqlConnectionString = PostgresConnectionString.Normalize(connectionString);
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(npgsqlConnectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IShowRepository, ShowRepository>();
        services.AddScoped<IShowSeatRepository, ShowSeatRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddScoped<DataSeeder>();

        return services;
    }
}
