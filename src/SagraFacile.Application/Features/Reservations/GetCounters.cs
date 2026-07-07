using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class GetCounters
{
    public record Query(int EventId) : IQuery<IList<ReservationCounterDto>>;
    public class Handler(IReservationRepository repository) : IQueryHandler<Query, IList<ReservationCounterDto>>
    {
        public async Task<IList<ReservationCounterDto>> Handle(Query query, CancellationToken cancellationToken)
        {
            var counters = await repository.GetCountersAsync(query.EventId, cancellationToken).ConfigureAwait(false);
            return counters.Select(c => new ReservationCounterDto(c.Status, c.Count, c.TotalPeople)).ToList();
        }
    }
}