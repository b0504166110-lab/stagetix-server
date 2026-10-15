using Microsoft.EntityFrameworkCore;
using StageTix.Core.Entities;

namespace StageTix.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Show> Shows => Set<Show>();

    public DbSet<ShowSeat> ShowSeats => Set<ShowSeat>();

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Every IEntityTypeConfiguration<T> in the Configurations folder.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RefreshConcurrencyTokens();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RefreshConcurrencyTokens();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Gives every new or changed seat a new Version. EF Core still sends the ORIGINAL Version
    /// in the WHERE clause of the UPDATE, so if another request saved the seat after we read it,
    /// zero rows are updated and EF Core throws DbUpdateConcurrencyException.
    /// </summary>
    private void RefreshConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries<ShowSeat>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.Version = Guid.NewGuid();
            }
        }
    }
}
