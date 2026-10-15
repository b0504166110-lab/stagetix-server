using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Data;

namespace StageTix.Tests.Concurrency;

/// <summary>
/// A real, throw-away PostgreSQL database ("stagetix_tests") for the concurrency tests.
/// It is created from the migrations before the tests and dropped after them, so the
/// developer's demo database is never touched.
///
/// Connection: the STAGETIX_TEST_DB environment variable, or else the developer's User Secrets
/// connection string with the database name replaced. Without either, the tests are skipped (not failed).
/// </summary>
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private const string TestDatabaseName = "stagetix_tests";

    private DbContextOptions<AppDbContext>? _options;

    public string? SkipReason { get; private set; }

    public bool IsAvailable => SkipReason is null;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<PostgresDatabaseFixture>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["STAGETIX_TEST_DB"] ?? configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            SkipReason = "No PostgreSQL connection string (set STAGETIX_TEST_DB or the API's User Secrets).";
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = TestDatabaseName };
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(builder.ConnectionString).Options;

        try
        {
            await using var db = CreateContext();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException)
        {
            SkipReason = $"PostgreSQL is not reachable: {ex.Message}";
        }
    }

    public async Task DisposeAsync()
    {
        if (IsAvailable)
        {
            await using var db = CreateContext();
            await db.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>A brand-new DbContext with its own connection — like a separate HTTP request.</summary>
    public AppDbContext CreateContext() =>
        new(_options ?? throw new InvalidOperationException(SkipReason));

    /// <summary>Creates an on-sale show with all seats available, and returns its id.</summary>
    public async Task<int> CreateShowAsync(int rows = 2, int seatsPerRow = 5)
    {
        await using var db = CreateContext();
        var show = new Show
        {
            Title = "Concurrency test",
            Artist = "Test",
            StartsAt = DateTime.UtcNow.AddDays(10),
            TicketPrice = 50,
            Category = ShowCategory.Music,
            Status = ShowStatus.OnSale,
            CreatedAt = DateTime.UtcNow
        };
        show.GenerateSeats(rows, seatsPerRow);
        db.Shows.Add(show);
        await db.SaveChangesAsync();
        return show.Id;
    }

    /// <summary>Creates a customer and returns her id.</summary>
    public async Task<int> CreateCustomerAsync()
    {
        await using var db = CreateContext();
        var user = new User
        {
            FullName = "Test Customer",
            Email = $"customer-{Guid.NewGuid():N}@example.com",
            PasswordHash = "not-used",
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }
}

[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "PostgreSQL";
}
