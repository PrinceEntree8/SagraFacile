using NSubstitute;
using SagraFacile.Application.Features.Events;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;

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
            new UpdateEventAdditionalOptions.Command(1, true, 6),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(ev.AdditionalOptions.Reservations.PartyCompletion.Enabled);
        Assert.Equal(6, ev.AdditionalOptions.Reservations.PartyCompletion.MinPartySize);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventNotFound_ReturnsFailure()
    {
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Event?)null);

        var result = await _handler.Handle(
            new UpdateEventAdditionalOptions.Command(99, false, 8),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("EventNotFound", result.Error);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MinPartySizeZero_ReturnsValidationFailure()
    {
        var result = await _handler.Handle(
            new UpdateEventAdditionalOptions.Command(1, true, 0),
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
            new UpdateEventAdditionalOptions.Command(1, true, -5),
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
            new UpdateEventAdditionalOptions.Command(1, false, 1),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, ev.AdditionalOptions.Reservations.PartyCompletion.MinPartySize);
    }
}
