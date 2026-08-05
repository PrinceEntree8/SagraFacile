using SagraFacile.Domain.Common;
using SagraFacile.Domain.Features.Orders;
using Xunit;

namespace SagraFacile.Application.Tests.Features.Orders;

public class OrderTransitionPolicyTests
{
    [Fact]
    public void DefaultPolicy_CanTransitionAndCheckAllowedFor()
    {
        var policy = OrderTransitionPolicy.CreateDefault();

        Assert.True(policy.CanTransition(OrderStatus.Draft, OrderStatus.Confirmed));
        Assert.True(policy.CanTransition(OrderStatus.Confirmed, OrderStatus.Fulfilled));
        Assert.False(policy.CanTransition(OrderStatus.Draft, OrderStatus.Delivered));

        Assert.True(policy.IsAllowedFor(OrderStatus.Draft, OrderStatus.Confirmed, OrderActor.Cashier));
        Assert.False(policy.IsAllowedFor(OrderStatus.Draft, OrderStatus.Confirmed, OrderActor.Customer));

        Assert.True(policy.IsAllowedFor(OrderStatus.Draft, OrderStatus.Confirmed, OrderActor.System));
    }

    [Fact]
    public void DefaultPolicy_IsEditable_OnlyDraftAndPreorder()
    {
        var policy = OrderTransitionPolicy.CreateDefault();

        Assert.True(policy.IsEditable(OrderStatus.Draft));
        Assert.True(policy.IsEditable(OrderStatus.Preorder));
        Assert.False(policy.IsEditable(OrderStatus.Confirmed));
        Assert.False(policy.IsEditable(OrderStatus.Delivered));
    }

    [Fact]
    public void CustomPolicy_AllowEditAfterConfirmation_AllowsConfirmedEdits()
    {
        var policy = new OrderTransitionPolicy(
            OrderStatusRules.Default,
            allowEditAfterConfirmation: true,
            requireReasonOnRejection: true,
            requireReasonOnCancellation: false,
            allowFollowUpOrders: true,
            autoConfirmPreorders: false,
            defaultConfirmerRole: OrderActor.Cashier);

        Assert.True(policy.IsEditable(OrderStatus.Confirmed));
    }
}
