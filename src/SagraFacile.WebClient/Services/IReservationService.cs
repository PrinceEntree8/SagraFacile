using Refit;
using SagraFacile.Contracts.Reservations;

namespace SagraFacile.WebClient.Services;

public interface IReservationService
{
    [Get("/api/reservations")]
    Task<ReservationsDto> GetReservationsAsync(int eventId, string? status = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    
    [Get("/api/reservations/{id}")]
    Task<ReservationDto> GetReservationAsync(int id, CancellationToken ct = default);

    [Get("/api/reservations/last-called")]
    Task<IReadOnlyList<CalledEntry>> GetLastCalledReservationsAsync(int eventId, int maxEntries = 10, CancellationToken ct = default);

    [Post("/api/reservations")]
    Task<ReservationCommandResponse> CreateAsync([Body] CreateReservationRequest request, CancellationToken ct = default);

    [Post("/api/reservations/{id}/call")]
    Task<ReservationCommandResponse> CallAsync(int id, [Body] CallReservationRequest request, CancellationToken ct = default);

    [Post("/api/reservations/{id}/seat")]
    Task<ReservationCommandResponse> SeatAsync(int id, CancellationToken ct = default);

    [Post("/api/reservations/call-and-seat")]
    Task<ReservationCommandResponse> CallAndSeatAsync([Body] CallAndSeatRequest request, CancellationToken ct = default);

    [Delete("/api/reservations/{id}")]
    Task<ReservationCommandResponse> VoidAsync(int id, CancellationToken ct = default);

    [Post("/api/reservations/{id}/restore")]
    Task<ReservationCommandResponse> RestoreAsync(int id, CancellationToken ct = default);

    [Put("/api/reservations/{id}")]
    Task<ReservationCommandResponse> EditAsync(int id, [Body] EditReservationRequest req, CancellationToken ct = default);

    [Post("/api/reservations/{id}/party-complete")]
    Task<ReservationCommandResponse> MarkPartyCompleteAsync(int id, CancellationToken ct = default);

    [Get("/api/reservations/counters")]
    Task<IReadOnlyList<ReservationCounterDto>> GetCountersAsync(int eventId, CancellationToken ct = default);

    [Get("/api/reservations/best-fit")]
    Task<IReadOnlyList<ReservationMatchDto>> GetBestFitAsync(int eventId, int availableSeats, CancellationToken ct = default);

    [Get("/api/reservations/report")]
    Task<IReadOnlyList<ReservationReportDto>> GetReportAsync(int eventId, CancellationToken ct = default);
}
