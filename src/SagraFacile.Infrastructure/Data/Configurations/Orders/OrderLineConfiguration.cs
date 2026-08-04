using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Infrastructure.Data.Configurations.Orders;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();

        entity.Ignore(e => e.TotalInCents);

        entity.Property(e => e.MenuItemName).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Notes).HasMaxLength(500);

        entity.HasIndex(e => new { e.OrderId, e.Position }).HasDatabaseName("IX_OrderLines_OrderId_Position");
    }
}
