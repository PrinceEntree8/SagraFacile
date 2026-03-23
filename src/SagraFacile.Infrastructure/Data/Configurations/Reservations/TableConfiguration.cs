using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Infrastructure.Data.Configurations.Reservations;

public class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.TableNumber).IsRequired().HasMaxLength(50);
        entity.Property(e => e.CoverCount).IsRequired();
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.HasIndex(e => e.TableNumber).IsUnique();
    }
}
