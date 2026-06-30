namespace SagraFacile.Application.Features.Reservations;

internal static class ReservationTimestampNormalization
{
    public static DateTime AsUtc(this DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public static DateTime? AsUtc(this DateTime? value)
    {
        return value.HasValue ? value.Value.AsUtc() : null;
    }
}