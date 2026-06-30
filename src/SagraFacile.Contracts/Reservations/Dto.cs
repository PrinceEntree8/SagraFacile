namespace SagraFacile.Contracts.Reservations;

public record ReservationDto(
    int Id,
    int SequenceNumber,
    string CustomerName,
    int PartySize,
    string Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime? FirstCalledAt,
    DateTime? LastCalledAt,
    int CallCount,
    TimeSpan WaitingTime,
    TimeSpan? TimeSinceLastCall);
    
public record ReservationsDto(List<ReservationDto> Reservations, int TotalCount);

public record ReservationCounterDto(string Status, int Count, int TotalPeople);

public record ReservationReportDto(
    int Id,
    int SequenceNumber,
    string CustomerName,
    int PartySize,
    string Status,
    DateTime CreatedAt,
    DateTime? FirstCalledAt,
    DateTime? SeatedAt,
    DateTime? VoidedAt,
    int CallCount,
    TimeSpan? WaitTimeUntilFirstCall,
    TimeSpan? TotalWaitTime); 
    
    
public record ReservationMatchDto(
    int Id,
    int SequenceNumber,
    string CustomerName,
    int PartySize,
    string? Notes,
    DateTime CreatedAt,
    TimeSpan WaitingTime,
    int CallCount,
    DateTime? LastCalledAt,
    string MatchQuality);
    
public record CalledEntry(int Id, int SequenceNumber, string CustomerName, int PartySize);

public record CreateReservationResult(int Id, int SequenceNumber);