using NSubstitute;
using SagraFacile.Application.Features.Events;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Tests.Features.Events;

public class UpdateEventAdditionalOptionsHandlerTests
{
    private readonly IEventRepository _repository = Substitute.For<IEventRepository>();
    private readonly UpdateEventAdditionalOptions.Handler _handler;

    public UpdateEventAdditionalOptionsHandlerTests()
        => _handler = new UpdateEventAdditionalOptions.Handler(_repository);

    [Fact]
    public async Task Handle_ValidCommand_UpdatesOptionsAndSaves()
    {
        var ev = new Event { Id = 1, Name = "Test" };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ev);

        var result = await _handler.Handle(
            new UpdateEventAdditionalOptions.Command(1, true, 6, true, false, true, 30, [OrderContext.Table, OrderContext.Takeaway], true, 200, OrderActor.Supervisor, true, false),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(ev.AdditionalOptions.Reservations.PartyCompletion.Enabled);
        Assert.Equal(6, ev.AdditionalOptions.Reservations.PartyCompletion.MinPartySize);
        Assert.True(ev.AdditionalOptions.View.ShowNotesField);
        Assert.False(ev.AdditionalOptions.View.CounterPeopleFirst);
        Assert.True(ev.AdditionalOptions.View.ShowCallCount);
        Assert.Equal(30, ev.AdditionalOptions.View.MaxWaitTimeMinutes);
        Assert.Equal(2, ev.AdditionalOptions.Orders.EnabledContexts.Count);
        Assert.True(ev.AdditionalOptions.Orders.CoverChargeEnabled);
        Assert.Equal(200, ev.AdditionalOptions.Orders.DefaultCoverChargeInCents);
        Assert.Equal(OrderActor.Supervisor, ev.AdditionalOptions.Orders.Lifecycle.DefaultConfirmerRole);
        Assert.True(ev.AdditionalOptions.Orders.Lifecycle.AllowEditAfterConfirmation);
        Assert.False(ev.AdditionalOptions.Orders.Lifecycle.AllowFollowUpOrders);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventNotFound_ReturnsFailure()
    {
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Event?)null);

        var result = await _handler.Handle(
            new UpdateEventAdditionalOptions.Command(99, false, 8, false, true, false, 45, [OrderContext.Table], false, 0, OrderActor.Cashier, false, true),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("EventNotFound", result.Error);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MinPartySizeZero_ReturnsValidationFailure()
    {
        var result = await _handler.Handle(
            new UpdateEventAdditionalOptions.Command(1, true, 0, false, true, false, 45, [OrderContext.Table], false, 0, OrderActor.Cashier, false, true),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("MinPartySizeInvalid", result.Error);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MinPartySizeNegative_ReturnsValidationFailure()
    {
        var result = await _handler.Handle(
            new UpdateEventAdditionalOptions.Command(1, true, -5, false, true, false, 45, [OrderContext.Table], false, 0, OrderActor.Cashier, false, true),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("MinPartySizeInvalid", result.Error);
    }

    [Fact]
    public async Task Handle_MinPartySizeOne_IsValid()
    {
        var ev = new Event { Id = 1, Name = "Test" };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ev);

        var result = await _handler.Handle(
            new UpdateEventAdditionalOptions.Command(1, false, 1, false, true, false, 45, [OrderContext.Table], false, 0, OrderActor.Cashier, false, true),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, ev.AdditionalOptions.Reservations.PartyCompletion.MinPartySize);
    }

    [Fact]
    public async Task Handle_MaxWaitTimeMinutesZero_ReturnsValidationFailure()
    {
        var result = await _handler.Handle(
            new UpdateEventAdditionalOptions.Command(1, true, 8, false, true, false, 0, [OrderContext.Table], false, 0, OrderActor.Cashier, false, true),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("MaxWaitTimeMinutesInvalid", result.Error);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyEnabledContexts_ReturnsValidationFailure()
    {
        var result = await _handler.Handle(
            new UpdateEventAdditionalOptions.Command(1, true, 8, false, true, false, 30, [], false, 0, OrderActor.Cashier, false, true),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("EnabledContextsRequired", result.Error);
    }

    [Fact]
    public async Task Handle_NegativeCoverCharge_ReturnsValidationFailure()
    {
        var result = await _handler.Handle(
            new UpdateEventAdditionalOptions.Command(1, true, 8, false, true, false, 30, [OrderContext.Table], true, -1, OrderActor.Cashier, false, true),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("CoverChargeInvalid", result.Error);
    }
}
