using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Infrastructure.Data.Configurations.Menu;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> entity)
    {
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Name).IsRequired().HasMaxLength(200);
        entity.Property(m => m.Description).HasMaxLength(1000);
        entity.Property(m => m.Price).HasColumnType("decimal(18,2)");
        entity.Property(m => m.Category).IsRequired();
        entity.Property(m => m.DisplayOrder).HasDefaultValue(0);
        entity.Property(m => m.IsAvailable).HasDefaultValue(true);
    }
}
