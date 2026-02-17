using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;

namespace SagraFacile.Web.Features.Reservations;

public static class GetTables
{
    public record Query(string? Status = null);

    public record Result(List<TableDto> Tables);

    public record TableDto(int Id, string TableNumber, int CoverCount, string Status, DateTime CreatedAt, DateTime? UpdatedAt);

    public static async Task<Result> Handle(Query query, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var queryable = context.Tables.AsQueryable();

        if (!string.IsNullOrEmpty(query.Status))
        {
            queryable = queryable.Where(t => t.Status == query.Status);
        }

        var tables = await queryable
            .OrderBy(t => t.TableNumber)
            .Select(t => new TableDto(
                t.Id,
                t.TableNumber,
                t.CoverCount,
                t.Status,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new Result(tables);
    }
}
