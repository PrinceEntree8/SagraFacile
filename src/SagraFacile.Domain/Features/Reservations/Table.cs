namespace SagraFacile.Domain.Features.Reservations;

public class Table
{
    public int Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int CoverCount { get; set; }
    public string Status { get; set; } = "Available"; // Available, Occupied, Reserved
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
