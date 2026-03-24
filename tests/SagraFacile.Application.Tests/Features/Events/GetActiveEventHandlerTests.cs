using NSubstitute;
using SagraFacile.Application.Features.Events;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Application.Tests.Features.Events;

public class GetActiveEventHandlerTests
{
    private readonly IEventRepository _repository = Substitute.For<IEventRepository>();
    private readonly GetActiveEvent.Handler _handler;

    public GetActiveEventHandlerTests()
    {
        _handler = new GetActiveEvent.Handler(_repository);
    }

    [Fact]
    public async Task Handle_WhenActiveEventExists_ReturnsEventDto()
    {
        var ev = new Event { Id = 1, Name = "Sagra 2026", Currency = "EUR", CurrencySymbol = "€", IsActive = true, Date = DateTime.UtcNow };
        _repository.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(ev);

        var result = await _handler.Handle(new GetActiveEvent.Query(), CancellationToken.None);

        Assert.NotNull(result.ActiveEvent);
        Assert.Equal(1, result.ActiveEvent!.Id);
        Assert.Equal("Sagra 2026", result.ActiveEvent.Name);
        Assert.Equal("EUR", result.ActiveEvent.Currency);
        Assert.Equal("€", result.ActiveEvent.CurrencySymbol);
    }

    [Fact]
    public async Task Handle_WhenNoActiveEvent_ReturnsNullDto()
    {
        _repository.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((Event?)null);

        var result = await _handler.Handle(new GetActiveEvent.Query(), CancellationToken.None);

        Assert.Null(result.ActiveEvent);
    }
}
