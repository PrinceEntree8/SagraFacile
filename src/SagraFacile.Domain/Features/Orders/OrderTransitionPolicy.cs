namespace SagraFacile.Domain.Features.Orders;

public sealed class OrderTransitionPolicy
{
    private readonly Dictionary<(OrderStatus From, OrderStatus To), HashSet<OrderActor>> _edges;

    public bool AllowEditAfterConfirmation { get; }
    public bool RequireReasonOnRejection { get; }
    public bool RequireReasonOnCancellation { get; }
    public bool AllowFollowUpOrders { get; }
    public bool AutoConfirmPreorders { get; }
    public OrderActor DefaultConfirmerRole { get; }

    public OrderTransitionPolicy(
        IEnumerable<OrderTransitionRule> rules,
        bool allowEditAfterConfirmation,
        bool requireReasonOnRejection,
        bool requireReasonOnCancellation,
        bool allowFollowUpOrders,
        bool autoConfirmPreorders,
        OrderActor defaultConfirmerRole)
    {
        _edges = rules.ToDictionary(r => (r.From, r.To), r => r.Actors.ToHashSet());
        AllowEditAfterConfirmation = allowEditAfterConfirmation;
        RequireReasonOnRejection = requireReasonOnRejection;
        RequireReasonOnCancellation = requireReasonOnCancellation;
        AllowFollowUpOrders = allowFollowUpOrders;
        AutoConfirmPreorders = autoConfirmPreorders;
        DefaultConfirmerRole = defaultConfirmerRole;
    }

    public static OrderTransitionPolicy CreateDefault() => new(
        OrderStatusRules.Default, false, true, false, true, false, OrderActor.Cashier);

    public bool CanTransition(OrderStatus from, OrderStatus to) => _edges.ContainsKey((from, to));

    public bool IsAllowedFor(OrderStatus from, OrderStatus to, OrderActor actor)
        => actor == OrderActor.System
        || (_edges.TryGetValue((from, to), out var actors) && actors.Contains(actor));

    public IReadOnlyCollection<OrderStatus> AllowedTargets(OrderStatus from, OrderActor? actor = null)
        => _edges.Where(e => e.Key.From == from && (actor is null || e.Value.Contains(actor.Value)))
                 .Select(e => e.Key.To).ToList();

    public bool IsEditable(OrderStatus status) => status switch
    {
        OrderStatus.Draft or OrderStatus.Preorder => true,
        OrderStatus.Confirmed => AllowEditAfterConfirmation,
        _ => false
    };

    public bool RequiresReason(OrderStatus to) => to switch
    {
        OrderStatus.Rejected => RequireReasonOnRejection,
        OrderStatus.CancelledByCustomer or OrderStatus.CancelledByOperator => RequireReasonOnCancellation,
        _ => false
    };
}
