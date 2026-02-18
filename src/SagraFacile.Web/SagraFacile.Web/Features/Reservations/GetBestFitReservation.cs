using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;
using SagraFacile.Web.Infrastructure.CQRS;

namespace SagraFacile.Web.Features.Reservations;

public static class GetBestFitReservation
{
    public record Query(int TableCoverCount) : IQuery<Result>;

    public record Result(List<ReservationMatchDto> Matches);

    public record ReservationMatchDto(
        int Id,
        string QueueNumber,
        string CustomerName,
        int PartySize,
        string Notes,
        DateTime CreatedAt,
        TimeSpan WaitingTime,
        int CallCount,
        DateTime? LastCalledAt,
        string MatchQuality); // Perfect, Good, Acceptable

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
            
            // Get only CALLED reservations (head waiter works with called customers)
            var reservations = await _context.TableReservations
                .Where(r => r.Status == "Called")
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            var matches = new List<ReservationMatchDto>();

            // If coverCount is 0, return ALL Called reservations without filtering
            if (query.TableCoverCount == 0)
            {
                foreach (var reservation in reservations)
                {
                    matches.Add(new ReservationMatchDto(
                        reservation.Id,
                        reservation.QueueNumber,
                        reservation.CustomerName,
                        reservation.PartySize,
                        reservation.Notes,
                        reservation.CreatedAt,
                        now - reservation.CreatedAt,
                        reservation.CallCount,
                        reservation.LastCalledAt,
                        "All")); // Use "All" to indicate no filtering
                }
                
                return new Result(matches);
            }

            // Otherwise, apply best-fit algorithm with cover count filter
            foreach (var reservation in reservations)
            {
                // Best fit algorithm:
                // - Perfect match: party size equals table cover
                // - Good match: party size is 1-2 less than cover
                // - Acceptable: party size is 3+ less than cover (with margin)
                
                string? matchQuality = null;
                
                if (reservation.PartySize == query.TableCoverCount)
                {
                    matchQuality = "Perfect";
                }
                else if (reservation.PartySize >= query.TableCoverCount - 2 && reservation.PartySize <= query.TableCoverCount)
                {
                    matchQuality = "Good";
                }
                else if (reservation.PartySize <= query.TableCoverCount && reservation.PartySize >= query.TableCoverCount - 4)
                {
                    matchQuality = "Acceptable";
                }

                // Only include if it fits
                if (matchQuality != null && reservation.PartySize <= query.TableCoverCount)
                {
                    matches.Add(new ReservationMatchDto(
                        reservation.Id,
                        reservation.QueueNumber,
                        reservation.CustomerName,
                        reservation.PartySize,
                        reservation.Notes,
                        reservation.CreatedAt,
                        now - reservation.CreatedAt,
                        reservation.CallCount,
                        reservation.LastCalledAt,
                        matchQuality));
                }
            }

            // Sort by match quality first, then by waiting time (FIFO - first in, first out)
            var sortedMatches = matches
                .OrderBy(m => m.MatchQuality == "Perfect" ? 0 : m.MatchQuality == "Good" ? 1 : 2)
                .ThenBy(m => m.CreatedAt)
                .ToList();

            return new Result(sortedMatches);
        }
    }
}
