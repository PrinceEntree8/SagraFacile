using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Contracts.Reservations;

/// <summary>Unified status-change payload. CallCount is only set when NewStatus == Called.</summary>
public record ReservationStatusChangedNotification(
    int ReservationId,
    int SequenceNumber,
    string CustomerName,
    int PartySize,
    ReservationStatus NewStatus,
    ReservationStatus? OldStatus = null,
    int? CallCount = null
);

/// <summary>Aggregated seat/queue counters for the event.</summary>
public record CountersUpdatedNotification(
    List<ReservationCounterDto> Counters
);

public abstract record ReservationNotificationMessage;
public record StatusChangedMessage(ReservationStatusChangedNotification Notification) : ReservationNotificationMessage;
public record CountersUpdatedMessage(CountersUpdatedNotification Notification) : ReservationNotificationMessage;