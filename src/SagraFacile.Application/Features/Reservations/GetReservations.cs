using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class GetReservations
{
    public record Query(int EventId, string? Status = null, int Page = 1, int PageSize = 50, ReservationStatusFilter Filter = ReservationStatusFilter.AllWaiting) : IQuery<Result>;
    public record Result(List<ReservationDto> Reservations, int TotalCount);

    public record ReservationDto(
        int Id,
        int SequenceNumber,
        string CustomerName,
        int PartySize,
        string Status,
        string? Notes,
        DateTime CreatedAt,
        DateTime? FirstCalledAt,
        DateTime? LastCalledAt,
        int CallCount,
        TimeSpan WaitingTime,
        TimeSpan? TimeSinceLastCall);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly IReservationRepository _repository;

        public Handler(IReservationRepository repository) => _repository = repository;

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var (items, total) = await _repository.GetPagedAsync(query.EventId, query.Status, query.Page, query.PageSize, query.Filter, cancellationToken);

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

            return new Result(dtos, total);
        }
    }
}
