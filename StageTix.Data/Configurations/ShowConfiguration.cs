using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StageTix.Core.Entities;

namespace StageTix.Data.Configurations;

public class ShowConfiguration : IEntityTypeConfiguration<Show>
{
    public void Configure(EntityTypeBuilder<Show> builder)
    {
        builder.ToTable("Shows", t => t.HasCheckConstraint("CK_Shows_TicketPrice_Positive", "\"TicketPrice\" > 0"));

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Artist).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.StartsAt).IsRequired();
        builder.Property(s => s.TicketPrice).HasPrecision(10, 2).IsRequired();
        builder.Property(s => s.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        // One-to-many: Show -> ShowSeats. Seats exist only as part of their show.
        builder.HasMany(s => s.Seats)
            .WithOne(seat => seat.Show)
            .HasForeignKey(seat => seat.ShowId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: Show -> Orders. A show with orders cannot be deleted (history is kept).
        builder.HasMany(s => s.Orders)
            .WithOne(o => o.Show)
            .HasForeignKey(o => o.ShowId)
            .OnDelete(DeleteBehavior.Restrict);

        // The show list filters by status and sorts by date.
        builder.HasIndex(s => new { s.Status, s.StartsAt });
    }
}
