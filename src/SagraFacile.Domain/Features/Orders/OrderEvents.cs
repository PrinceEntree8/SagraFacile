using SagraFacile.Domain.Common;

namespace SagraFacile.Domain.Features.Orders;

public record OrderCreated(int OrderId, int EventId, int OrderNumber, OrderContext Context, int? ContextReferenceId)
    : IDomainEvent { public DateTime OccurredAtUtc { get; } = DateTime.UtcNow; }

public record OrderLinesChanged(int OrderId, int EventId, int LineCount, int TotalInCents)
    : IDomainEvent { public DateTime OccurredAtUtc { get; } = DateTime.UtcNow; }

public record OrderStatusChanged(int OrderId, int EventId, OrderStatus From, OrderStatus To, string? UserId)
    : IDomainEvent { public DateTime OccurredAtUtc { get; } = DateTime.UtcNow; }

public record OrderConfirmed(int OrderId, int EventId, int TotalInCents)
    : IDomainEvent { public DateTime OccurredAtUtc { get; } = DateTime.UtcNow; }

public record OrderCancelled(int OrderId, int EventId, string? Reason)
    : IDomainEvent { public DateTime OccurredAtUtc { get; } = DateTime.UtcNow; }

public record OrderCompleted(int OrderId, int EventId)
    : IDomainEvent { public DateTime OccurredAtUtc { get; } = DateTime.UtcNow; }
