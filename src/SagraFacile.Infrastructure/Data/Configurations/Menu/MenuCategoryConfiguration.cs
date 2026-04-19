using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Infrastructure.Data.Configurations.Menu;

public class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> entity)
    {
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
        entity.Property(c => c.DisplayOrder).HasDefaultValue(0);
    }
}
