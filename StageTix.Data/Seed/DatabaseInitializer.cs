using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StageTix.Data.Seed;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations and, if requested, seeds demo data into an empty database.
    /// Called once at startup, so the system runs without any manual database step
    /// (locally and in the cloud).
    /// </summary>
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services, bool seedDemoData, CancellationToken cancellationToken = default)
    {
        // DbContext is Scoped, so it must be resolved inside a scope (there is no HTTP request yet).
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));
        var db = provider.GetRequiredService<AppDbContext>();

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
        }

        await db.Database.MigrateAsync(cancellationToken);

        if (seedDemoData)
        {
            await provider.GetRequiredService<DataSeeder>().SeedAsync(cancellationToken);
        }
    }
}
