using SagraFacile.Contracts.Common;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Contracts.Reservations;

public interface IReservationHubClient
{
    Task ReservationStatusChanged(ReservationStatusChangedNotification notification);
    Task CountersUpdated(List<ReservationCounterDto> counters);
    Task AvailableSeatsUpdated(int availableSeats);

    Task<ReservationCommandResponse> CreateReservation(
        int eventId,
        string customerName,
        int partySize,
        string? notes = null,
        bool partyComplete = false);

    Task<ReservationCommandResponse> EditReservation(
        int id,
        string? customerName = null,
        int? partySize = null,
        string? notes = null,
        ReservationStatus? status = null);

    Task<ReservationCommandResponse> CallReservation(
        int reservationId,
        string calledBy = "Receptionist",
        string? notes = null);

    Task<ReservationCommandResponse> MarkPartyComplete(
        int reservationId,
        string markedBy = "System");

    Task<ReservationCommandResponse> SeatReservation(int reservationId);
    Task<ReservationCommandResponse> CallAndSeatReservation(int eventId, int sequenceNumber);

    Task<ReservationCommandResponse> VoidReservation(int reservationId);
    Task<ReservationCommandResponse> RestoreReservation(int reservationId);

    Task<CommandResult> UpdateTableCover(
        int? tableId,
        string? tableNumber,
        int coverCount);
}
