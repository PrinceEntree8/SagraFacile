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
            var r = await repository.GetByIdAsync(query.Id, cancellationToken);

            if (r is null)
            {
                return null;
            }

            return ReservationDtoMapper.Map(r);
        }
    }
}
