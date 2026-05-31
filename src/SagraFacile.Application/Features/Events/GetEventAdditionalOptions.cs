using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Application.Features.Events;

public static class GetEventAdditionalOptions
{
    public record Query(int EventId) : IQuery<Result?>;

    public record Result(int EventId, AdditionalOptionsDto AdditionalOptions);

    public record AdditionalOptionsDto(ReservationOptionsDto Reservations);

    public record ReservationOptionsDto(PartyCompletionOptionsDto PartyCompletion);

    public record PartyCompletionOptionsDto(bool Enabled, int MinPartySize);

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
                            opts.Reservations.PartyCompletion.MinPartySize))));
        }
    }
}
