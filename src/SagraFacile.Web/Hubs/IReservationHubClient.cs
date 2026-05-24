using SagraFacile.Application.Features.Reservations;

namespace SagraFacile.Web.Hubs;

public interface IReservationHubClient
{
    Task ReservationCreated(int reservationId, int sequenceNumber, string customerName, int partySize);
    Task ReservationCalled(int reservationId, int sequenceNumber, string customerName, int partySize, int callCount);
    Task ReservationVoided(int reservationId, int sequenceNumber);
    Task ReservationSeated(int reservationId, int sequenceNumber);
    Task AvailableSeatsUpdated(int availableSeats);
    Task CountersUpdated(List<GetCounters.ReservationCounter> counters);
}
