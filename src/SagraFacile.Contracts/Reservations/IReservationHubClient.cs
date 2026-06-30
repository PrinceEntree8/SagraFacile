using SagraFacile.Contracts.Common;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Contracts.Reservations;

public interface IReservationHubClient
{
    Task ReservationStatusChanged(ReservationStatusChangedNotification notification);
    Task CountersUpdated(List<ReservationCounterDto> counters);
    Task AvailableSeatsUpdated(int availableSeats);

    Task<CommandResult<CreateReservationResult>> CreateReservation(
        int eventId,
        string customerName,
        int partySize,
        string? notes = null,
        bool partyComplete = false);

    Task<CommandResult> EditReservation(
        int id,
        string? customerName = null,
        int? partySize = null,
        string? notes = null,
        ReservationStatus? status = null);

    Task<CommandResult> CallReservation(
        int reservationId,
        string calledBy = "Receptionist",
        string? notes = null);

    Task<CommandResult> MarkPartyComplete(
        int reservationId,
        string markedBy = "System");

    Task<CommandResult> SeatReservation(int reservationId);
    Task<CommandResult> CallAndSeatReservation(int eventId, int sequenceNumber);

    Task<CommandResult> VoidReservation(int reservationId);

    Task<CommandResult> UpdateTableCover(
        int? tableId,
        string? tableNumber,
        int coverCount);
}
