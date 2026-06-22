using System.Net.Http.Json;
using SagraFacile.Contracts.Common;
using SagraFacile.Contracts.Reservations;

namespace SagraFacile.WebClient.Services;

public class ReservationService(HttpClient httpClient) : IReservationService
{
    public async Task<(IReadOnlyList<ReservationDto> Reservations, int TotalCount)> GetReservationsAsync(int eventId, string? status, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = $"api/reservations?eventId={eventId}&page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status))
            query += $"&status={Uri.EscapeDataString(status)}";

        var response = await httpClient.GetFromJsonAsync<GetReservationsResponse>(query, ct);
        return (response?.Reservations ?? [], response?.TotalCount ?? 0);
    }
    public async Task<(IReadOnlyList<ReservationDto> Reservations, int TotalCount)> GetLastCalledReservationsAsync(int eventId, CancellationToken ct = default)
    {
        var query = $"api/reservations/last-called?eventId={eventId}";

        var response = await httpClient.GetFromJsonAsync<GetReservationsResponse>(query, ct);
        return (response?.Reservations ?? [], response?.TotalCount ?? 0);
    }

    public async Task<CommandResult<(int Id, int SequenceNumber)>> CreateAsync(CreateReservationRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/reservations", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommandResult<(int Id, int SequenceNumber)>>(cancellationToken: ct) ?? throw new InvalidOperationException();
    }

    public async Task<CommandResult> CallAsync(int id, CallReservationRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"api/reservations/{id}/call", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommandResult>(cancellationToken: ct) ?? throw new InvalidOperationException();
    }

    public async Task<CommandResult> SeatAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"api/reservations/{id}/seat", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommandResult>(cancellationToken: ct) ?? throw new InvalidOperationException();
    }

    public async Task<CommandResult> CallAndSeatAsync(CallAndSeatRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/reservations/call-and-seat", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommandResult>(cancellationToken: ct) ?? throw new InvalidOperationException();
    }

    public async Task<CommandResult> VoidAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"api/reservations/{id}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommandResult>(cancellationToken: ct) ?? throw new InvalidOperationException();
    }

    public async Task<CommandResult> EditAsync(int id, EditReservationRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/reservations/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommandResult>(cancellationToken: ct) ?? throw new InvalidOperationException();
    }

    public async Task<CommandResult> MarkPartyCompleteAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"api/reservations/{id}/party-complete", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommandResult>(cancellationToken: ct) ?? throw new InvalidOperationException();
    }

    public async Task<IReadOnlyList<ReservationCounterDto>> GetCountersAsync(int eventId, CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<ReservationCounterDto>>($"api/reservations/counters?eventId={eventId}", ct) ?? [];

    public async Task<IReadOnlyList<ReservationMatchDto>> GetBestFitAsync(int eventId, int availableSeats, CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<ReservationMatchDto>>($"api/reservations/best-fit?eventId={eventId}&availableSeats={availableSeats}", ct) ?? [];

    public async Task<IReadOnlyList<ReservationReportDto>> GetReportAsync(int eventId, CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<ReservationReportDto>>($"api/reservations/report?eventId={eventId}", ct) ?? [];

    private sealed record GetReservationsResponse(List<ReservationDto> Reservations, int TotalCount);
}
