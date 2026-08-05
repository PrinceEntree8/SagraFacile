using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Infrastructure.Data.Configurations.Orders;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();

        entity.Ignore(e => e.DomainEvents);
        entity.Ignore(e => e.LinesTotalInCents);
        entity.Ignore(e => e.TotalInCents);

        entity.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        entity.Property(e => e.Context).IsRequired().HasConversion<string>().HasMaxLength(20);
        entity.Property(e => e.ContextLabel).HasMaxLength(200);
        entity.Property(e => e.CustomerName).HasMaxLength(200);
        entity.Property(e => e.Notes).HasMaxLength(500);
        entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
        entity.Property(e => e.Version).IsConcurrencyToken();

        entity.HasIndex(e => new { e.EventId, e.OrderNumber })
              .IsUnique()
              .HasDatabaseName("IX_Orders_EventId_OrderNumber");
        entity.HasIndex(e => new { e.EventId, e.Status }).HasDatabaseName("IX_Orders_EventId_Status");
        entity.HasIndex(e => new { e.Context, e.ContextReferenceId }).HasDatabaseName("IX_Orders_Context_ContextReferenceId");
        entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Orders_CreatedAt");
        entity.HasIndex(e => e.ParentOrderId).HasDatabaseName("IX_Orders_ParentOrderId");

        entity.HasOne(e => e.Event)
              .WithMany()
              .HasForeignKey(e => e.EventId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Parent)
              .WithMany(e => e.FollowUps)
              .HasForeignKey(e => e.ParentOrderId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(e => e.Lines)
              .WithOne(l => l.Order)
              .HasForeignKey(l => l.OrderId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.Transitions)
              .WithOne(t => t.Order)
              .HasForeignKey(t => t.OrderId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
