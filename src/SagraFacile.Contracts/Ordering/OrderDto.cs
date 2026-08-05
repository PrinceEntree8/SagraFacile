using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Contracts.Ordering;

public record OrderDto(
    int Id,
    int EventId,
    int OrderNumber,
    OrderStatus Status,
    OrderContext Context,
    int? ContextReferenceId,
    string? ContextLabel,
    int Covers,
    int CoverChargeInCents,
    int TotalInCents,
    string? CustomerName,
    string? Notes,
    int? ParentOrderId,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    DateTime? FulfilledAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    IReadOnlyCollection<OrderLineDto> Lines,
    IReadOnlyCollection<OrderTransitionDto> History,
    IReadOnlyCollection<OrderStatus> AllowedTransitions);

public record OrderLineDto(
    int Id,
    int MenuItemId,
    string MenuItemName,
    int UnitPriceInCents,
    int Quantity,
    int TotalInCents,
    string? Notes,
    int Position);

public record OrderTransitionDto(
    OrderStatus FromStatus,
    OrderStatus ToStatus,
    DateTime OccurredAt,
    string? UserId,
    OrderActor ActorRole,
    string? Reason);

public record OrderSummaryDto(
    int Id,
    int EventId,
    int OrderNumber,
    OrderStatus Status,
    OrderContext Context,
    string? ContextLabel,
    int Covers,
    int TotalInCents,
    string? CustomerName,
    DateTime CreatedAt);
