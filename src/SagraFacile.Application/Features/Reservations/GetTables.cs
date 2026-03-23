using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Reservations;

public static class GetTables
{
    public record Query(string? Status = null) : IQuery<Result>;
    public record Result(List<TableDto> Tables);
    public record TableDto(int Id, string TableNumber, int CoverCount, string Status, DateTime CreatedAt, DateTime? UpdatedAt);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly ITableRepository _repository;

        public Handler(ITableRepository repository) => _repository = repository;

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            var tables = await _repository.GetAllAsync(query.Status, cancellationToken);
            var dtos = tables
                .Select(t => new TableDto(t.Id, t.TableNumber, t.CoverCount, t.Status, t.CreatedAt, t.UpdatedAt))
                .ToList();
            return new Result(dtos);
        }
    }
}
