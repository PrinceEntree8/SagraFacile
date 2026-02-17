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
        int Priority,
        DateTime CreatedAt,
        TimeSpan WaitingTime,
        int CallCount,
        DateTime? LastCalledAt,
        string MatchQuality); // Perfect, Good, Acceptable

    public static async Task<Result> Handle(Query query, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        
        // Get all waiting or called reservations
        var reservations = await context.TableReservations
            .Where(r => r.Status == "Waiting" || r.Status == "Called")
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
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
                    reservation.Priority,
                    reservation.CreatedAt,
                    now - reservation.CreatedAt,
                    reservation.CallCount,
                    reservation.LastCalledAt,
                    matchQuality));
            }
        }

        // Sort by priority first, then by match quality, then by waiting time
        var sortedMatches = matches
            .OrderByDescending(m => m.Priority)
            .ThenBy(m => m.MatchQuality == "Perfect" ? 0 : m.MatchQuality == "Good" ? 1 : 2)
            .ThenBy(m => m.CreatedAt)
            .ToList();

        return new Result(sortedMatches);
    }
}
