using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Features.Events;

public static class GetEventAdditionalOptions
{
    public record Query(int EventId) : IQuery<Result?>;

    public record Result(int EventId, AdditionalOptionsDto AdditionalOptions);

    public record AdditionalOptionsDto(ReservationOptionsDto Reservations, ViewOptionsDto View, OrderOptionsDto Orders);

    public record ReservationOptionsDto(PartyCompletionOptionsDto PartyCompletion);

    public record PartyCompletionOptionsDto(bool Enabled, int MinPartySize);

    public record ViewOptionsDto(bool ShowNotesField, bool CounterPeopleFirst, bool ShowCallCount, int MaxWaitTimeMinutes);

    public record OrderOptionsDto(
        IReadOnlyCollection<OrderContext> EnabledContexts,
        bool CoverChargeEnabled,
        int DefaultCoverChargeInCents,
        OrderActor DefaultConfirmerRole,
        bool AllowEditAfterConfirmation,
        bool AllowFollowUpOrders);

    public class Handler : IQueryHandler<Query, Result?>
    {
        private readonly IEventRepository _repository;

        public Handler(IEventRepository repository) => _repository = repository;

        public async Task<Result?> Handle(Query query, CancellationToken cancellationToken)
        {
            var ev = await _repository.GetByIdAsync(query.EventId, cancellationToken);
            if (ev is null) return null;

            return MapToResult(ev);
        }

        internal static Result MapToResult(Event ev)
        {
            var opts = ev.AdditionalOptions;
            return new Result(
                ev.Id,
                new AdditionalOptionsDto(
                    new ReservationOptionsDto(
                        new PartyCompletionOptionsDto(
                            opts.Reservations.PartyCompletion.Enabled,
                            opts.Reservations.PartyCompletion.MinPartySize)),
                    new ViewOptionsDto(
                        opts.View.ShowNotesField,
                        opts.View.CounterPeopleFirst,
                        opts.View.ShowCallCount,
                        opts.View.MaxWaitTimeMinutes),
                    new OrderOptionsDto(
                        opts.Orders.EnabledContexts,
                        opts.Orders.CoverChargeEnabled,
                        opts.Orders.DefaultCoverChargeInCents,
                        opts.Orders.Lifecycle.DefaultConfirmerRole,
                        opts.Orders.Lifecycle.AllowEditAfterConfirmation,
                        opts.Orders.Lifecycle.AllowFollowUpOrders)));
        }
    }
}
