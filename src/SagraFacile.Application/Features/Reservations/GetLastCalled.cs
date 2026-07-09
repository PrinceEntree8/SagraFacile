using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public class GetLastCalled
{
    public record Query(int EventId, int MaxEntries) : IQuery<IList<CalledEntry>>;
    public class Handler(IReservationRepository repository) : IQueryHandler<Query, IList<CalledEntry>>
    {
        public async Task<IList<CalledEntry>> Handle(Query query, CancellationToken cancellationToken)
        {
            var reservations = await repository.GetLastCalledAsync(query.EventId, query.MaxEntries, cancellationToken).ConfigureAwait(false);
            return reservations.Select(r => new CalledEntry(r.Id, r.SequenceNumber, r.CustomerName, r.PartySize, r.CallCount)).ToList();
        }
    }
}