namespace SagraFacile.Web.Hubs;

public interface IReservationHubClient
{
    Task ReservationCreated(int reservationId, string queueNumber, string customerName, int partySize);
    Task ReservationCalled(int reservationId, string queueNumber, int callCount);
    Task ReservationVoided(int reservationId, string queueNumber);
    Task ReservationSeated(int reservationId, string queueNumber);
    Task TableUpdated(int tableId, string tableNumber, int coverCount);
}
