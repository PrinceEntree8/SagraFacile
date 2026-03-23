using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Infrastructure.Data.Configurations.Events;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Description).HasMaxLength(1000);
        entity.Property(e => e.Currency).IsRequired().HasMaxLength(10);
        entity.Property(e => e.CurrencySymbol).IsRequired().HasMaxLength(5);
    }
}
