namespace SagraFacile.Domain.Features.Reservations;

public class TableReservation
{
    public int Id { get; set; }
    public string QueueNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public string Status { get; set; } = "Waiting"; // Waiting, Called, Seated, Voided
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FirstCalledAt { get; set; }
    public DateTime? LastCalledAt { get; set; }
    public DateTime? SeatedAt { get; set; }
    public DateTime? VoidedAt { get; set; }
    public int CallCount { get; set; } = 0;

    /// <summary>
    /// Optimistic concurrency token. Incremented automatically by the repository on every write.
    /// EF Core includes this in the WHERE clause of UPDATE statements; if another transaction has
    /// already committed a change (bumping the version), SaveChanges throws DbUpdateConcurrencyException.
    /// </summary>
    public long Version { get; set; } = 0;

    public ICollection<ReservationCall> Calls { get; set; } = new List<ReservationCall>();
}
