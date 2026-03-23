using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;
using SagraFacile.Web.Infrastructure.CQRS;

namespace SagraFacile.Web.Features.Events;

public static class ActivateEvent
{
    public record Command(int EventId) : ICommand<Result>;
    public record Result(bool Success, string Message);

    public class Handler : ICommandHandler<Command, Result>
    {
        private readonly ApplicationDbContext _db;
        public Handler(ApplicationDbContext db) => _db = db;

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var ev = await _db.Events.FindAsync([command.EventId], cancellationToken);
            if (ev == null) return new Result(false, "Event not found");

            await _db.Events.Where(e => e.IsActive).ExecuteUpdateAsync(s => s.SetProperty(e => e.IsActive, false), cancellationToken);
            ev.IsActive = true;
            await _db.SaveChangesAsync(cancellationToken);
            return new Result(true, $"Event '{ev.Name}' activated");
        }
    }
}
