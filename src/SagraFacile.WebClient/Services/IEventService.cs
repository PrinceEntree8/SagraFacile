using Refit;
using SagraFacile.Contracts.Events;

namespace SagraFacile.WebClient.Services;

public interface IEventService
{
    [Get("/api/events")]
    Task<IReadOnlyList<EventDto>> GetEventsAsync(CancellationToken ct = default);

    [Post("/api/events")]
    Task<CreateEventResponse> CreateEventAsync([Body] CreateEventRequest request, CancellationToken ct = default);

    [Put("/api/events/{eventId}/activate")]
    Task<ActivateEventResponse> ActivateEventAsync(int eventId, CancellationToken ct = default);

    [Get("/api/events/active")]
    Task<EventDto?> GetActiveEventAsync(CancellationToken ct = default);

    [Get("/api/events/{eventId}/options")]
    Task<EventAdditionalOptionsDto?> GetEventOptionsAsync(int eventId, CancellationToken ct = default);

    [Put("/api/events/{eventId}/options")]
    Task<UpdateEventOptionsResponse> UpdateEventOptionsAsync(int eventId, [Body] UpdateEventAdditionalOptionsRequest request, CancellationToken ct = default);
}
