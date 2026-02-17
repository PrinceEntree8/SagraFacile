using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;

namespace SagraFacile.Web.Features.Reservations;

public static class GetBestFitReservation
{
    public record Query(int TableCoverCount);

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

    public static async Task<Result> Handle(Query query, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        
        // Get only CALLED reservations (head waiter works with called customers)
        var reservations = await context.TableReservations
            .Where(r => r.Status == "Called")
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var matches = new List<ReservationMatchDto>();

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
