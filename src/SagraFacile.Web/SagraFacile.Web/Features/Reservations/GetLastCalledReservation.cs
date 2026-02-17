using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;

namespace SagraFacile.Web.Features.Reservations;

public record GetLastCalledReservation;

public record LastCalledReservationDto(
    string QueueNumber,
    string CustomerName,
    int PartySize,
    DateTime LastCalledAt
);

public static class GetLastCalledReservationHandler
{
    public static async Task<LastCalledReservationDto?> Handle(
        GetLastCalledReservation query,
        ApplicationDbContext db)
    {
        var lastCalled = await db.TableReservations
            .Where(r => r.LastCalledAt != null)
            .OrderByDescending(r => r.LastCalledAt)
            .Select(r => new LastCalledReservationDto(
                r.QueueNumber,
                r.CustomerName,
                r.PartySize,
                r.LastCalledAt!.Value
            ))
            .FirstOrDefaultAsync();

        return lastCalled;
    }
    
    public static async Task<List<LastCalledReservationDto>> HandleMultiple(
        GetLastCalledReservation query,
        ApplicationDbContext db,
        int count = 5)
    {
        var lastCalled = await db.TableReservations
            .Where(r => r.LastCalledAt != null)
            .OrderByDescending(r => r.LastCalledAt)
            .Take(count)
            .Select(r => new LastCalledReservationDto(
                r.QueueNumber,
                r.CustomerName,
                r.PartySize,
                r.LastCalledAt!.Value
            ))
            .ToListAsync();

        return lastCalled;
    }
}
