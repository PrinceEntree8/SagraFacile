using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Contracts.Ordering;

public record CreateDraftOrderRequest(
    OrderContext Context,
    int? ContextReferenceId,
    string? ContextLabel,
    int Covers,
    string? CustomerName,
    string? Notes,
    IReadOnlyCollection<OrderLineRequest> Lines);

public record SubmitPreorderRequest(
    OrderContext Context,
    int? ContextReferenceId,
    string? ContextLabel,
    int Covers,
    string? CustomerName,
    string? Notes,
    IReadOnlyCollection<OrderLineRequest> Lines);

public record OrderLineRequest(int MenuItemId, int Quantity, string? Notes = null);

public record RejectOrderRequest(string Reason);

public record CancelOrderRequest(string? Reason = null);

public record AdvanceOrderRequest(OrderStatus? TargetStatus = null, string? Reason = null);

public record CreateFollowUpOrderRequest(IReadOnlyCollection<OrderLineRequest> Lines);
