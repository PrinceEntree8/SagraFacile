using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Infrastructure.Data.Configurations.Reservations;

public class ReservationCallConfiguration : IEntityTypeConfiguration<ReservationCall>
{
    public void Configure(EntityTypeBuilder<ReservationCall> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.CalledBy).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Notes).IsRequired().HasMaxLength(500);
    }
}
