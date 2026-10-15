using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StageTix.Core.Entities;

namespace StageTix.Data.Configurations;

public class ShowSeatConfiguration : IEntityTypeConfiguration<ShowSeat>
{
    public void Configure(EntityTypeBuilder<ShowSeat> builder)
    {
        builder.ToTable("ShowSeats", t =>
        {
            t.HasCheckConstraint("CK_ShowSeats_Row_Positive", "\"Row\" > 0");
            t.HasCheckConstraint("CK_ShowSeats_Number_Positive", "\"Number\" > 0");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Row).IsRequired();
        builder.Property(s => s.Number).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // The concurrency token: EF Core adds "AND Version = <value we read>" to every UPDATE/DELETE
        // of a seat. AppDbContext replaces the value on every save.
        builder.Property(s => s.Version).IsConcurrencyToken().IsRequired();

        // The same physical seat cannot appear twice in one show.
        builder.HasIndex(s => new { s.ShowId, s.Row, s.Number }).IsUnique();
    }
}
