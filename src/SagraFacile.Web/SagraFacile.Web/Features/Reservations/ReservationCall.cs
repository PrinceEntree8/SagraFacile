namespace SagraFacile.Web.Features.Reservations;

public class ReservationCall
{
    public int Id { get; set; }
    public int TableReservationId { get; set; }
    public DateTime CalledAt { get; set; } = DateTime.UtcNow;
    public string CalledBy { get; set; } = "System"; // Who called the reservation
    public string Notes { get; set; } = string.Empty;
    
    public TableReservation TableReservation { get; set; } = null!;
}
