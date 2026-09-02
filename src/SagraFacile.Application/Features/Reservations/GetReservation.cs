using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class GetReservation
{
    public record Query(
        int Id
    ) : IQuery<ReservationDto?>;

    public class Handler(IReservationRepository repository) : IQueryHandler<Query, ReservationDto?>
    {
        public async Task<ReservationDto?> Handle(Query query, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var r = await repository.GetByIdAsync(query.Id, cancellationToken);

            if (r is null)
            {
                return null;
            }

            return new ReservationDto(
                r.Id,
                r.SequenceNumber,
                r.CustomerName,
                r.PartySize,
                r.Status.ToString(),
                r.Notes,
                r.CreatedAt.AsUtc(),
                r.FirstCalledAt.AsUtc(),
                r.LastCalledAt.AsUtc(),
                r.CallCount,
                now - r.CreatedAt.AsUtc(),
                r.LastCalledAt.HasValue ? now - r.LastCalledAt.Value.AsUtc() : null);
        }
    }
}