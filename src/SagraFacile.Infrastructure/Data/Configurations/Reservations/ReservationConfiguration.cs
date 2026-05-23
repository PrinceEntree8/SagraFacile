using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Infrastructure.Data.Configurations.Reservations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> entity)
    {
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id).ValueGeneratedOnAdd();

        entity.HasIndex(e => new { e.EventId, e.SequenceNumber })
              .IsUnique()
              .HasDatabaseName("IX_Reservations_EventId_SequenceNumber");

        entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
        entity.Property(e => e.PartySize).IsRequired();

        entity.Property(e => e.Status)
              .IsRequired()
              .HasConversion<string>()
              .HasMaxLength(20);

        entity.Property(e => e.Notes)
              .HasMaxLength(500);

        entity.Property(e => e.Version).IsConcurrencyToken();

        entity.HasOne(e => e.Event)
              .WithMany()
              .HasForeignKey(e => e.EventId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(e => e.Calls)
              .WithOne(c => c.Reservation)
              .HasForeignKey(c => c.ReservationId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
