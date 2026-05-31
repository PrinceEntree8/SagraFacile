using NSubstitute;
using SagraFacile.Application.Features.Events;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Application.Tests.Features.Events;

public class GetEventAdditionalOptionsHandlerTests
{
    private readonly IEventRepository _repository = Substitute.For<IEventRepository>();
    private readonly GetEventAdditionalOptions.Handler _handler;

    public GetEventAdditionalOptionsHandlerTests()
        => _handler = new GetEventAdditionalOptions.Handler(_repository);

    [Fact]
    public async Task Handle_EventExists_ReturnsOptions()
    {
        var ev = new Event
        {
            Id = 1,
            Name = "Test",
            AdditionalOptions = new EventAdditionalOptions
            {
                Reservations = new ReservationOptions
                {
                    PartyCompletion = new PartyCompletionOptions { Enabled = true, MinPartySize = 10 }
                }
            }
        };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ev);

        var result = await _handler.Handle(new GetEventAdditionalOptions.Query(1), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.EventId);
        Assert.True(result.AdditionalOptions.Reservations.PartyCompletion.Enabled);
        Assert.Equal(10, result.AdditionalOptions.Reservations.PartyCompletion.MinPartySize);
    }

    [Fact]
    public async Task Handle_EventNotFound_ReturnsNull()
    {
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Event?)null);

        var result = await _handler.Handle(new GetEventAdditionalOptions.Query(99), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_EventWithDefaultOptions_ReturnsSafeDefaults()
    {
        var ev = new Event { Id = 2, Name = "Legacy" };
        _repository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(ev);

        var result = await _handler.Handle(new GetEventAdditionalOptions.Query(2), CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.AdditionalOptions.Reservations.PartyCompletion.Enabled);
        Assert.True(result.AdditionalOptions.Reservations.PartyCompletion.MinPartySize >= 1);
    }
}
