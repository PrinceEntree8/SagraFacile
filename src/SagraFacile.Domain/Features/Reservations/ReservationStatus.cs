namespace SagraFacile.Domain.Features.Reservations;

public enum ReservationStatus
{
    None = 0,
    Waiting = 1,
    Called  = 2,
    Seated  = 3,
    Voided  = 4,
    PartyCompleted = 5,
    Left = 6,
}

[Flags]
public enum ReservationStatusFilter
{
    None = 0,
    Waiting = 1 << 1,
    Called = 1 << 2,
    Seated = 1 << 3,
    Voided = 1  << 4,
    PartyCompleted = 1 << 5,
    Left = 1 << 6,
    
    // Composite
    Default = AllWaiting,
    All = Waiting | Called | Seated | Voided | PartyCompleted | Left,
    AllWaiting = Waiting | PartyCompleted | Called,
    AllCompleted = Seated | Left,
    Active = AllWaiting | AllCompleted,
}

public static class ReservationFilterExtensions
{
    private static readonly (ReservationStatus Status, ReservationStatusFilter Filter)[] Map =
    [
        (ReservationStatus.Waiting, ReservationStatusFilter.Waiting),
        (ReservationStatus.Called, ReservationStatusFilter.Called),
        (ReservationStatus.Seated, ReservationStatusFilter.Seated),
        (ReservationStatus.Voided, ReservationStatusFilter.Voided),
        (ReservationStatus.PartyCompleted, ReservationStatusFilter.PartyCompleted),
        (ReservationStatus.Left, ReservationStatusFilter.Left),
    ];

    public static bool HasFlagFast(this ReservationStatusFilter value, ReservationStatusFilter flag)
        => (value & flag) != 0;

    public static IList<ReservationStatus> ToStatusArray(this ReservationStatusFilter values)
    {
        if (values == ReservationStatusFilter.None)
            return [];

        Span<ReservationStatus> buffer = stackalloc ReservationStatus[Map.Length];
        var count = 0;

        foreach (var (status, filter) in Map)
        {
            if ((values & filter) != 0)
                buffer[count++] = status;
        }

        return buffer[..count].ToArray();
    }
}