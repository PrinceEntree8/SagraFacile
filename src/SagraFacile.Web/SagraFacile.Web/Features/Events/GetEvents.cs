using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;
using SagraFacile.Web.Infrastructure.CQRS;

namespace SagraFacile.Web.Features.Events;

public static class GetEvents
{
    public record Query() : IQuery<Result>;
    public record Result(List<EventDto> Events);
    public record EventDto(int Id, string Name, string Description, DateTime Date, string Currency, string CurrencySymbol, bool IsActive, DateTime CreatedAt);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly ApplicationDbContext _db;
        public Handler(ApplicationDbContext db) => _db = db;

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            var events = await _db.Events
                .OrderByDescending(e => e.Date)
                .Select(e => new EventDto(e.Id, e.Name, e.Description, e.Date, e.Currency, e.CurrencySymbol, e.IsActive, e.CreatedAt))
                .ToListAsync(cancellationToken);
            return new Result(events);
        }
    }
}
