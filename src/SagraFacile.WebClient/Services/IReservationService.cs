using SagraFacile.Contracts.Common;
using SagraFacile.Contracts.Reservations;

namespace SagraFacile.WebClient.Services;

public interface IReservationService
{
    Task<(IReadOnlyList<ReservationDto> Reservations, int TotalCount)> GetReservationsAsync(int eventId, string? status, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<IReadOnlyList<CalledEntry>> GetLastCalledReservationsAsync(int eventId, CancellationToken ct = default);
    Task<CommandResult<(int Id, int SequenceNumber)>> CreateAsync(CreateReservationRequest request, CancellationToken ct = default);
    Task<CommandResult> CallAsync(int id, CallReservationRequest request, CancellationToken ct = default);
    Task<CommandResult> SeatAsync(int id, CancellationToken ct = default);
    Task<CommandResult> CallAndSeatAsync(CallAndSeatRequest request, CancellationToken ct = default);
    Task<CommandResult> VoidAsync(int id, CancellationToken ct = default);
    Task<CommandResult> RestoreAsync(int id, CancellationToken ct = default);
    Task<CommandResult> EditAsync(int id, EditReservationRequest request, CancellationToken ct = default);
    Task<CommandResult> MarkPartyCompleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ReservationCounterDto>> GetCountersAsync(int eventId, CancellationToken ct = default);
    Task<IReadOnlyList<ReservationMatchDto>> GetBestFitAsync(int eventId, int availableSeats, CancellationToken ct = default);
    Task<IReadOnlyList<ReservationReportDto>> GetReportAsync(int eventId, CancellationToken ct = default);
}
