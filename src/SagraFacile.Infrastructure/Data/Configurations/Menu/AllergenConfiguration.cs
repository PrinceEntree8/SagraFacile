using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Infrastructure.Data.Configurations.Menu;

public class AllergenConfiguration : IEntityTypeConfiguration<Allergen>
{
    public void Configure(EntityTypeBuilder<Allergen> entity)
    {
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Code).IsRequired().HasMaxLength(50);
        entity.Property(a => a.Icon).IsRequired().HasMaxLength(20);
        entity.HasIndex(a => a.Code).IsUnique();
    }
}
