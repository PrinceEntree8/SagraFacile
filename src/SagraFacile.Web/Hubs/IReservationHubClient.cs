using SagraFacile.Application.Features.Reservations;

namespace SagraFacile.Web.Hubs;

public interface IReservationHubClient
{
    Task ReservationStatusChanged(ReservationStatusChangedNotification notification);
    Task CountersUpdated(List<GetCounters.ReservationCounter> counters);
    Task AvailableSeatsUpdated(int availableSeats);
}
