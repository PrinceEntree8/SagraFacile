using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Infrastructure.Data.Configurations.Reservations;

public class TableReservationConfiguration : IEntityTypeConfiguration<TableReservation>
{
    public void Configure(EntityTypeBuilder<TableReservation> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Ignore(e => e.Date);
        entity.Ignore(e => e.QueueNumber);
        entity.Property(e => e.ReservationId).IsRequired().HasMaxLength(50).HasColumnName("QueueNumber");
        entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.PartySize).IsRequired();
        entity.Property(e => e.Notes).HasMaxLength(500);
        entity.Property(e => e.Version).IsConcurrencyToken();
        entity.HasIndex(e => e.ReservationId).IsUnique();
        entity.HasMany(e => e.Calls)
            .WithOne(c => c.TableReservation)
            .HasForeignKey(c => c.TableReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
