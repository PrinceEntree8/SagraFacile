using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class GetReservations
{
    public record Query(
        int EventId,
        int Page = 1,
        int PageSize = 50,
        ReservationStatusFilter Filter = ReservationStatusFilter.AllWaiting
    ) : IQuery<ReservationsDto>;

    public class Handler(IReservationRepository repository) : IQueryHandler<Query, ReservationsDto>
    {
        public async Task<ReservationsDto> Handle(Query query, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var (items, total) = await repository.GetPagedAsync(query.EventId, query.Page, query.PageSize, query.Filter, cancellationToken);

            var dtos = items.Select(r => new ReservationDto(
                r.Id,
                r.SequenceNumber,
                r.CustomerName,
                r.PartySize,
                r.Status.ToString(),
                r.Notes,
                r.CreatedAt,
                r.FirstCalledAt,
                r.LastCalledAt,
                r.CallCount,
                now - r.CreatedAt,
                r.LastCalledAt.HasValue ? now - r.LastCalledAt.Value : null)).ToList();

            return new ReservationsDto(dtos, total);
        }
    }
}
