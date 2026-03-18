using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;
using SagraFacile.Web.Infrastructure.CQRS;

namespace SagraFacile.Web.Features.Reservations;

public static class GetReservations
{
    public record Query(string? Status = null, int Page = 1, int PageSize = 50) : IQuery<Result>;

    public record Result(List<ReservationDto> Reservations, int TotalCount);

    public record ReservationDto(
        int Id, 
        string QueueNumber, 
        string CustomerName, 
        int PartySize,
        string Status, 
        string Notes,
        DateTime CreatedAt,
        DateTime? FirstCalledAt,
        DateTime? LastCalledAt,
        int CallCount,
        TimeSpan WaitingTime,
        TimeSpan? TimeSinceLastCall);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly ApplicationDbContext _context;

        public Handler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var queryable = _context.TableReservations.AsQueryable();

            // Exclude seated and voided reservations by default (for receptionist view)
            // Both are terminal statuses
            if (string.IsNullOrEmpty(query.Status))
            {
                queryable = queryable.Where(r => r.Status != "Seated" && r.Status != "Voided");
            }
            else
            {
                queryable = queryable.Where(r => r.Status == query.Status);
            }

            var totalCount = await queryable.CountAsync(cancellationToken);

            var reservations = await queryable
                .OrderBy(r => r.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(r => new ReservationDto(
                    r.Id,
                    r.QueueNumber,
                    r.CustomerName,
                    r.PartySize,
                    r.Status,
                    r.Notes,
                    r.CreatedAt,
                    r.FirstCalledAt,
                    r.LastCalledAt,
                    r.CallCount,
                    now - r.CreatedAt,
                    r.LastCalledAt.HasValue ? now - r.LastCalledAt.Value : null))
                .ToListAsync(cancellationToken);

            return new Result(reservations, totalCount);
        }
    }
}
