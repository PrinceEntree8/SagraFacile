using SagraFacile.Contracts.Events;

namespace SagraFacile.WebClient.Services;

public interface IEventService
{
    Task<IReadOnlyList<EventDto>> GetEventsAsync(CancellationToken ct = default);
    Task<CreateEventResponse> CreateEventAsync(CreateEventRequest request, CancellationToken ct = default);
    Task<ActivateEventResponse> ActivateEventAsync(int eventId, CancellationToken ct = default);
    Task<EventDto?> GetActiveEventAsync(CancellationToken ct = default);
    Task<EventAdditionalOptionsDto?> GetEventOptionsAsync(int eventId, CancellationToken ct = default);
    Task<UpdateEventOptionsResponse> UpdateEventOptionsAsync(int eventId, UpdateEventAdditionalOptionsRequest request, CancellationToken ct = default);
}
