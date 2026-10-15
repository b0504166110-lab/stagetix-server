using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StageTix.Core.Entities;

namespace StageTix.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", t => t.HasCheckConstraint("CK_Orders_TotalPrice_NonNegative", "\"TotalPrice\" >= 0"));

        builder.HasKey(o => o.Id);

        builder.Property(o => o.TotalPrice).HasPrecision(10, 2).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();

        // One-to-many: User -> Orders. A user with orders cannot be deleted (history is kept).
        builder.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many-to-many: Order <-> ShowSeat through the join table "OrderSeats".
        builder.HasMany(o => o.Seats)
            .WithMany(s => s.Orders)
            .UsingEntity<Dictionary<string, object>>(
                "OrderSeats",
                seat => seat.HasOne<ShowSeat>().WithMany().HasForeignKey("ShowSeatId").OnDelete(DeleteBehavior.Restrict),
                order => order.HasOne<Order>().WithMany().HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("OrderSeats");
                    join.HasKey("OrderId", "ShowSeatId");
                });

        // "My orders" filters by user; cancelling a show finds its orders.
        builder.HasIndex(o => new { o.UserId, o.CreatedAt });
        builder.HasIndex(o => new { o.ShowId, o.Status });
    }
}
