using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Reservations;

public static class GetCounters
{
    public record Query(int EventId) : IQuery<Result>;
    public record Result(List<ReservationCounter> Counters);
    public record ReservationCounter(string Status, int Count, int TotalPeople);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly IReservationRepository _repository;

        public Handler(IReservationRepository repository) => _repository = repository;

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            var counters = await _repository.GetCountersAsync(query.EventId, cancellationToken).ConfigureAwait(false);
            return new Result(counters);
        }
    }
}