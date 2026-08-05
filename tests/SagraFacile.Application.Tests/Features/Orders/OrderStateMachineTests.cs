using Microsoft.Extensions.Logging.Abstractions;
using SagraFacile.Application.Features.Orders;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Orders;
using Xunit;

namespace SagraFacile.Application.Tests.Features.Orders;

public class OrderStateMachineTests
{
    [Fact]
    public void PolicyFor_DefaultEventOptions_ReturnsCanonicalPolicy()
    {
        var stateMachine = new OrderStateMachine(NullLogger<OrderStateMachine>.Instance);
        var @event = new Event { Id = 1, AdditionalOptions = new EventAdditionalOptions() };

        var policy = stateMachine.PolicyFor(@event);

        Assert.True(policy.CanTransition(OrderStatus.Draft, OrderStatus.Confirmed));
        Assert.True(policy.IsAllowedFor(OrderStatus.Draft, OrderStatus.Confirmed, OrderActor.Cashier));
        Assert.False(policy.IsAllowedFor(OrderStatus.Draft, OrderStatus.Confirmed, OrderActor.Customer));
    }

    [Fact]
    public void PolicyFor_WithTransitionOverrides_AppliesOverridesCorrectly()
    {
        var stateMachine = new OrderStateMachine(NullLogger<OrderStateMachine>.Instance);
        var @event = new Event
        {
            Id = 1,
            AdditionalOptions = new EventAdditionalOptions
            {
                Orders = new OrderOptions
                {
                    Lifecycle = new OrderLifecycleOptions
                    {
                        TransitionOverrides =
                        [
                            new OrderTransitionOverride
                            {
                                From = OrderStatus.Draft,
                                To = OrderStatus.Confirmed,
                                Enabled = false
                            }
                        ]
                    }
                }
            }
        };

        var policy = stateMachine.PolicyFor(@event);

        Assert.False(policy.CanTransition(OrderStatus.Draft, OrderStatus.Confirmed));
    }
}
