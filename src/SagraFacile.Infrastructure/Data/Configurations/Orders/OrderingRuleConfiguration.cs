using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Infrastructure.Data.Configurations.Orders;

public class OrderingRuleConfiguration : IEntityTypeConfiguration<OrderingRule>
{
    public void Configure(EntityTypeBuilder<OrderingRule> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedOnAdd();

        entity.Property(x => x.Scope).HasConversion<string>().HasMaxLength(20).IsRequired();
        entity.Property(x => x.TargetCode).HasMaxLength(50);
        entity.Property(x => x.Expression).IsRequired().HasMaxLength(500);
        entity.Property(x => x.ErrorMessage).HasMaxLength(500);
        entity.Property(x => x.WarningMessage).HasMaxLength(500);
        entity.Property(x => x.Priority).IsRequired();
        entity.Property(x => x.IsActive).IsRequired();

        entity.HasOne<Event>()
            .WithMany()
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.EventId, x.IsActive, x.Priority })
            .HasDatabaseName("IX_OrderingRules_EventId_IsActive_Priority");
    }
}
