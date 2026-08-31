using System.Net.Http.Json;
using SagraFacile.Contracts.Events;

namespace SagraFacile.WebClient.Services;

public class EventService(HttpClient httpClient) : IEventService
{
    public async Task<IReadOnlyList<EventDto>> GetEventsAsync(CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<EventDto>>("api/events", ct) ?? [];

    public async Task<CreateEventResponse> CreateEventAsync(CreateEventRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/events", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateEventResponse>(cancellationToken: ct) ?? throw new InvalidOperationException();
    }

    public async Task<ActivateEventResponse> ActivateEventAsync(int eventId, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsync($"api/events/{eventId}/activate", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ActivateEventResponse>(cancellationToken: ct) ?? throw new InvalidOperationException();
    }

    public Task<EventDto?> GetActiveEventAsync(CancellationToken ct = default)
        => httpClient.GetFromJsonAsync<EventDto?>("api/events/active", ct);

    public Task<EventAdditionalOptionsDto?> GetEventOptionsAsync(int eventId, CancellationToken ct = default)
        => httpClient.GetFromJsonAsync<EventAdditionalOptionsDto?>($"api/events/{eventId}/options", ct);

    public async Task<UpdateEventOptionsResponse> UpdateEventOptionsAsync(int eventId, UpdateEventAdditionalOptionsRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/events/{eventId}/options", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UpdateEventOptionsResponse>(cancellationToken: ct) ?? throw new InvalidOperationException();
    }
}
