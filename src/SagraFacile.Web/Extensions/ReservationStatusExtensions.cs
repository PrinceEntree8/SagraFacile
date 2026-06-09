using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Web.Extensions;

public static class ReservationStatusExtensions
{
    extension(ReservationStatus? status)
    {
        public string GetStatusClass() => status switch
        {
            ReservationStatus.Waiting => "reservation-status-waiting",
            ReservationStatus.PartyCompleted => "reservation-status-partycomplete",
            ReservationStatus.Called => "reservation-status-called",
            ReservationStatus.Seated => "reservation-status-seated",
            ReservationStatus.Voided => "reservation-status-voided",
            _ => "bg-secondary"
        };
    }

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