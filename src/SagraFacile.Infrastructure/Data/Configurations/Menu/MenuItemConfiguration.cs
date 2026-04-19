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
        entity.Property(m => m.PriceInCents).IsRequired();
        entity.Property(m => m.CategoryId).IsRequired();
        entity.Property(m => m.DisplayOrder).HasDefaultValue(0);
        entity.Property(m => m.IsAvailable).HasDefaultValue(true);
        entity.HasOne(m => m.Category)
            .WithMany(c => c.MenuItems)
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
