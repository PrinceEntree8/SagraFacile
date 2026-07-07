using SagraFacile.Contracts.Events;
using SagraFacile.Contracts.Common;

namespace SagraFacile.WebClient.Services;

public interface IEventService
{
    Task<IReadOnlyList<EventDto>> GetEventsAsync(CancellationToken ct = default);
    Task<CommandResult?> CreateEventAsync(CreateEventRequest request, CancellationToken ct = default);
    Task<CommandResult?> ActivateEventAsync(int eventId, CancellationToken ct = default);
    Task<EventDto?> GetActiveEventAsync(CancellationToken ct = default);
    Task<EventAdditionalOptionsDto?> GetEventOptionsAsync(int eventId, CancellationToken ct = default);
    Task<CommandResult?> UpdateEventOptionsAsync(int eventId, UpdateEventAdditionalOptionsRequest request, CancellationToken ct = default);
}
