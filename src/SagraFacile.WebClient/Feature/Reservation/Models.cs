namespace SagraFacile.WebClient.Feature.Reservation;

public sealed record EditReservationModel
{
    public int Id { get; init; }
    public string CustomerName { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "Waiting";
}