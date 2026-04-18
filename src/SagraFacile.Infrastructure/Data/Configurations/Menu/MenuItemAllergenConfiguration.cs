using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Infrastructure.Data.Configurations.Menu;

public class MenuItemAllergenConfiguration : IEntityTypeConfiguration<MenuItemAllergen>
{
    public void Configure(EntityTypeBuilder<MenuItemAllergen> entity)
    {
        entity.HasKey(mia => new { mia.MenuItemId, mia.AllergenId });
        entity.HasOne(mia => mia.MenuItem)
            .WithMany(m => m.MenuItemAllergens)
            .HasForeignKey(mia => mia.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(mia => mia.Allergen)
            .WithMany(a => a.MenuItemAllergens)
            .HasForeignKey(mia => mia.AllergenId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
