using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Extensions;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

internal static class ReservationDtoMapper
{
    public static ReservationDto Map(Reservation reservation)
    {
        var now = DateTime.UtcNow;
        return new ReservationDto(
            reservation.Id,
            reservation.SequenceNumber,
            reservation.CustomerName,
            reservation.PartySize,
            reservation.Status.ToString(),
            reservation.Notes,
            reservation.CreatedAt.AsUtc(),
            reservation.FirstCalledAt.AsUtc(),
            reservation.LastCalledAt.AsUtc(),
            reservation.CallCount,
            now - reservation.CreatedAt.AsUtc(),
            reservation.LastCalledAt.HasValue ? now - reservation.LastCalledAt.Value.AsUtc() : null);
    }
}
