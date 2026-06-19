namespace SagraFacile.WebClient.Extensions;

public static class ReservationStatusExtensions
{
    extension(string? status)
    {
        public string GetStatusClass() => status switch
        {
            "Waiting" => "reservation-status-waiting",
            "PartyCompleted" => "reservation-status-partycomplete",
            "Called" => "reservation-status-called",
            "Seated" => "reservation-status-seated",
            "Voided" => "reservation-status-voided",
            _ => "bg-secondary"
        };
    }
}