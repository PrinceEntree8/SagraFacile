using NSubstitute;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class GetTablesHandlerTests
{
    private readonly ITableRepository _repository = Substitute.For<ITableRepository>();
    private readonly GetTables.Handler _handler;

    public GetTablesHandlerTests()
    {
        _handler = new GetTables.Handler(_repository);
    }

    [Fact]
    public async Task Handle_NoFilter_ReturnsAllTablesAsDtos()
    {
        var now = DateTime.UtcNow;
        var tables = new List<Table>
        {
            new() { Id = 1, TableNumber = "T01", CoverCount = 4, Status = "Available", CreatedAt = now },
            new() { Id = 2, TableNumber = "T02", CoverCount = 6, Status = "Occupied", CreatedAt = now, UpdatedAt = now.AddMinutes(-5) },
        };
        _repository.GetAllAsync(null, Arg.Any<CancellationToken>()).Returns(tables);

        var result = await _handler.Handle(new GetTables.Query(), CancellationToken.None);

        Assert.Equal(2, result.Tables.Count);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_PassesFilterToRepository()
    {
        _repository.GetAllAsync("Available", Arg.Any<CancellationToken>()).Returns(new List<Table>());

        await _handler.Handle(new GetTables.Query(Status: "Available"), CancellationToken.None);

        await _repository.Received(1).GetAllAsync("Available", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MapsAllDtoFields()
    {
        var now = DateTime.UtcNow;
        var updated = now.AddMinutes(-3);
        var tables = new List<Table>
        {
            new() { Id = 5, TableNumber = "T05", CoverCount = 8, Status = "Occupied", CreatedAt = now, UpdatedAt = updated },
        };
        _repository.GetAllAsync(null, Arg.Any<CancellationToken>()).Returns(tables);

        var result = await _handler.Handle(new GetTables.Query(), CancellationToken.None);

        var dto = result.Tables[0];
        Assert.Equal(5, dto.Id);
        Assert.Equal("T05", dto.TableNumber);
        Assert.Equal(8, dto.CoverCount);
        Assert.Equal("Occupied", dto.Status);
        Assert.Equal(now, dto.CreatedAt);
        Assert.Equal(updated, dto.UpdatedAt);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        _repository.GetAllAsync(null, Arg.Any<CancellationToken>()).Returns(new List<Table>());

        var result = await _handler.Handle(new GetTables.Query(), CancellationToken.None);

        Assert.Empty(result.Tables);
    }
}
