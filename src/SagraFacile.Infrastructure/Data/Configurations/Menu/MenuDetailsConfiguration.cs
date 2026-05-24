using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Infrastructure.Data.Configurations.Menu;

public class MenuDetailsConfiguration : IEntityTypeConfiguration<MenuDetails>
{
    public void Configure(EntityTypeBuilder<MenuDetails> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.EventId).IsUnique();
        builder.Property(d => d.WarningMessage).HasColumnType("text");
        builder.Property(d => d.Header).HasColumnType("text");
        builder.Property(d => d.Footer).HasColumnType("text");
    }
}
