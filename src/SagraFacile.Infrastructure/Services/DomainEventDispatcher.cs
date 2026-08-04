using Microsoft.Extensions.DependencyInjection;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Common;

namespace SagraFacile.Infrastructure.Services;

public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            foreach (var handler in serviceProvider.GetServices(handlerType))
            {
                if (handler is null) continue;
                var method = handlerType.GetMethod("Handle")!;
                await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
