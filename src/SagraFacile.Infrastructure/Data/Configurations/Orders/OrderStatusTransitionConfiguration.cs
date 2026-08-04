using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Infrastructure.Data.Configurations.Orders;

public class OrderStatusTransitionConfiguration : IEntityTypeConfiguration<OrderStatusTransition>
{
    public void Configure(EntityTypeBuilder<OrderStatusTransition> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();

        entity.Property(e => e.FromStatus).IsRequired().HasConversion<string>().HasMaxLength(20);
        entity.Property(e => e.ToStatus).IsRequired().HasConversion<string>().HasMaxLength(20);
        entity.Property(e => e.UserId).HasMaxLength(450);
        entity.Property(e => e.Reason).HasMaxLength(500);

        entity.HasIndex(e => new { e.OrderId, e.OccurredAt }).HasDatabaseName("IX_OrderStatusTransitions_OrderId_OccurredAt");
    }
}
