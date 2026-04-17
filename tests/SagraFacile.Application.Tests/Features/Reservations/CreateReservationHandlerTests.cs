using NSubstitute;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class CreateReservationHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly CreateReservation.Handler _handler;

    public CreateReservationHandlerTests()
    {
        _handler = new CreateReservation.Handler(_repository);
    }

    [Fact]
    public async Task Handle_FirstReservationOfDay_GeneratesQueueNumberWithSequence0001()
    {
        // Arrange
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        _repository.GetLastByDatePrefixAsync(today, Arg.Any<CancellationToken>()).Returns((TableReservation?)null);

        TableReservation? saved = null;
        _repository.When(r => r.AddAsync(Arg.Any<TableReservation>(), Arg.Any<CancellationToken>()))
            .Do(ci => { saved = ci.Arg<TableReservation>(); saved.Id = 1; });

        var command = new CreateReservation.Command("Mario Rossi", 4);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("0001", result.QueueNumber);
        Assert.Equal(today, saved!.Date);
        Assert.Equal(1, result.Id);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SubsequentReservation_IncrementsSequenceNumber()
    {
        // Arrange
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var last = new TableReservation { Date = today, QueueNumber = "0005" };
        _repository.GetLastByDatePrefixAsync(today, Arg.Any<CancellationToken>()).Returns(last);

        _repository.When(r => r.AddAsync(Arg.Any<TableReservation>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<TableReservation>().Id = 2);

        var command = new CreateReservation.Command("Luigi Verdi", 2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("0006", result.QueueNumber);
    }

    [Fact]
    public async Task Handle_SetsStatusToWaiting()
    {
        // Arrange
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        _repository.GetLastByDatePrefixAsync(today, Arg.Any<CancellationToken>()).Returns((TableReservation?)null);

        TableReservation? saved = null;
        _repository.When(r => r.AddAsync(Arg.Any<TableReservation>(), Arg.Any<CancellationToken>()))
            .Do(ci => saved = ci.Arg<TableReservation>());

        // Act
        await _handler.Handle(new CreateReservation.Command("Test", 3), CancellationToken.None);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal("Waiting", saved!.Status);
    }
}
