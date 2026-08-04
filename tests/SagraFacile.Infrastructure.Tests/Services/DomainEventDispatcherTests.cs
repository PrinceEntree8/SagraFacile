using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Common;
using SagraFacile.Domain.Features.Orders;
using SagraFacile.Infrastructure.Services;
using Xunit;

namespace SagraFacile.Infrastructure.Tests.Services;

public class DomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ResolvesHandlersAndCallsHandle()
    {
        var handler = Substitute.For<IDomainEventHandler<OrderCreated>>();
        var services = new ServiceCollection();
        services.AddSingleton(handler);
        var provider = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(provider);
        var domainEvent = new OrderCreated(OrderId: 1, EventId: 1, OrderNumber: 10, Context: OrderContext.Takeaway, ContextReferenceId: null);

        await dispatcher.DispatchAsync([domainEvent]);

        await handler.Received(1).Handle(domainEvent, Arg.Any<CancellationToken>());
    }
}
