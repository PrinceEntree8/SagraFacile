namespace SagraFacile.Domain.Features.Reservations;

public class ReservationCall
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public DateTime CalledAt { get; set; }
    public string CalledBy { get; set; } = "System";
    public string? Notes { get; set; }

    public Reservation Reservation { get; set; } = null!;
}
