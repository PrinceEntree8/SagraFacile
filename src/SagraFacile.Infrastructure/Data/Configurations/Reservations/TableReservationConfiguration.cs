using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Infrastructure.Data.Configurations.Reservations;

public class TableReservationConfiguration : IEntityTypeConfiguration<TableReservation>
{
    public void Configure(EntityTypeBuilder<TableReservation> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.QueueNumber).IsRequired().HasMaxLength(50);
        entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.PartySize).IsRequired();
        entity.Property(e => e.Notes).HasMaxLength(500);
        entity.HasIndex(e => e.QueueNumber).IsUnique();
        entity.HasMany(e => e.Calls)
            .WithOne(c => c.TableReservation)
            .HasForeignKey(c => c.TableReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
