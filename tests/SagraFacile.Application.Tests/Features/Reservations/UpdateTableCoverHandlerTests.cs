using NSubstitute;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class UpdateTableCoverHandlerTests
{
    private readonly ITableRepository _repository = Substitute.For<ITableRepository>();
    private readonly UpdateTableCover.Handler _handler;

    public UpdateTableCoverHandlerTests()
    {
        _handler = new UpdateTableCover.Handler(_repository);
    }

    [Fact]
    public async Task Handle_ExistingTableById_UpdatesCoverCount()
    {
        var table = new Table { Id = 1, TableNumber = "T01", CoverCount = 4, Status = "Available", CreatedAt = DateTime.UtcNow };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(table);

        var result = await _handler.Handle(new UpdateTableCover.Command(1, null, 6), CancellationToken.None);

        Assert.Equal(6, result.CoverCount);
        Assert.Equal("T01", result.TableNumber);
        Assert.NotNull(table.UpdatedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingTableByNumber_UpdatesCoverCount()
    {
        var table = new Table { Id = 2, TableNumber = "T02", CoverCount = 6, Status = "Available", CreatedAt = DateTime.UtcNow };
        _repository.GetByNumberAsync("T02", Arg.Any<CancellationToken>()).Returns(table);

        var result = await _handler.Handle(new UpdateTableCover.Command(null, "T02", 8), CancellationToken.None);

        Assert.Equal(8, result.CoverCount);
        Assert.Equal("T02", result.TableNumber);
    }

    [Fact]
    public async Task Handle_NonExistentTable_CreatesNewTable()
    {
        _repository.GetByNumberAsync("T99", Arg.Any<CancellationToken>()).Returns((Table?)null);

        Table? added = null;
        _repository.When(r => r.AddAsync(Arg.Any<Table>(), Arg.Any<CancellationToken>()))
            .Do(ci => { added = ci.Arg<Table>(); added.Id = 99; });

        var result = await _handler.Handle(new UpdateTableCover.Command(null, "T99", 4), CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal("T99", added!.TableNumber);
        Assert.Equal(4, added.CoverCount);
        Assert.Equal("Available", added.Status);
        await _repository.Received(1).AddAsync(Arg.Any<Table>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentTableById_CreatesNewTableWithGeneratedNumber()
    {
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Table?)null);

        Table? added = null;
        _repository.When(r => r.AddAsync(Arg.Any<Table>(), Arg.Any<CancellationToken>()))
            .Do(ci => { added = ci.Arg<Table>(); added.Id = 1; });

        await _handler.Handle(new UpdateTableCover.Command(999, null, 3), CancellationToken.None);

        Assert.NotNull(added);
        // Auto-generated table number starts with 'T'
        Assert.StartsWith("T", added!.TableNumber);
    }

    [Fact]
    public void Validator_NeitherTableIdNorNumber_Fails()
    {
        var validator = new UpdateTableCover.Validator();
        var result = validator.Validate(new UpdateTableCover.Command(null, null, 4));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_ZeroCoverCount_Fails()
    {
        var validator = new UpdateTableCover.Validator();
        var result = validator.Validate(new UpdateTableCover.Command(1, null, 0));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTableCover.Command.CoverCount));
    }

    [Fact]
    public void Validator_CoverCountExceeds50_Fails()
    {
        var validator = new UpdateTableCover.Validator();
        var result = validator.Validate(new UpdateTableCover.Command(1, null, 51));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTableCover.Command.CoverCount));
    }

    [Fact]
    public void Validator_TableNumberTooLong_Fails()
    {
        var validator = new UpdateTableCover.Validator();
        var longNumber = new string('T', 51);
        var result = validator.Validate(new UpdateTableCover.Command(null, longNumber, 4));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_ValidCommandWithId_Passes()
    {
        var validator = new UpdateTableCover.Validator();
        var result = validator.Validate(new UpdateTableCover.Command(1, null, 4));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_ValidCommandWithTableNumber_Passes()
    {
        var validator = new UpdateTableCover.Validator();
        var result = validator.Validate(new UpdateTableCover.Command(null, "T01", 6));
        Assert.True(result.IsValid);
    }
}
