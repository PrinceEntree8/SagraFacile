using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Ordering;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Features.Orders;

public static class GetOrder
{
    public record Query(int OrderId, OrderActor? Actor = null) : IQuery<OrderDto?>;

    public class Handler(
        IOrderRepository repository,
        IOrderStateMachine stateMachine) : IQueryHandler<Query, OrderDto?>
    {
        public async Task<OrderDto?> Handle(Query query, CancellationToken ct)
        {
            var order = await repository.GetByIdWithHistoryAsync(query.OrderId, ct);
            if (order is null) return null;

            var policy = stateMachine.PolicyFor(order.Event);
            var allowed = policy.AllowedTargets(order.Status, query.Actor);

            var lines = order.Lines.Select(l => new OrderLineDto(
                l.Id, l.MenuItemId, l.MenuItemName, l.UnitPriceInCents, l.Quantity, l.TotalInCents, l.Notes, l.Position
            )).ToList();

            var history = order.Transitions.Select(t => new OrderTransitionDto(
                t.FromStatus, t.ToStatus, t.OccurredAt, t.UserId, t.ActorRole, t.Reason
            )).ToList();

            return new OrderDto(
                order.Id, order.EventId, order.OrderNumber, order.Status, order.Context,
                order.ContextReferenceId, order.ContextLabel, order.Covers, order.CoverChargeInCents,
                order.TotalInCents, order.CustomerName, order.Notes, order.ParentOrderId,
                order.CreatedAt, order.ConfirmedAt, order.FulfilledAt, order.CompletedAt, order.CancelledAt,
                lines, history, allowed.ToList()
            );
        }
    }
}
