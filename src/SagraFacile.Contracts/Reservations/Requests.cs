namespace SagraFacile.Contracts.Reservations;

public record CallAndSeatRequest(int EventId, int SequenceNumber);

public record CallReservationRequest(string CalledBy = "Receptionist", string? Notes = null);

public record CreateReservationRequest(
    int EventId,
    string CustomerName,
    int PartySize,
    string? Notes = null,
    bool PartyComplete = false
);

public record EditReservationRequest(string? CustomerName, int? PartySize, string? Notes);
