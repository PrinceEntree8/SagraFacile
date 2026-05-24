using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Menu;

public static class DeleteMenuItem
{
    public record Command(int Id) : ICommand<Result>;
    public record Result(bool Success, string Message);

    public class Handler(IMenuRepository repo, IMenuCacheService cache) : ICommandHandler<Command, Result>
    {
        public async Task<Result> Handle(Command command, CancellationToken ct)
        {
            var item = await repo.GetByIdAsync(command.Id, ct);
            if (item is null) return new Result(false, "Item not found");

            var eventId = item.EventId;
            await repo.DeleteAsync(command.Id, ct);
            await repo.SaveChangesAsync(ct);
            cache.InvalidateMenu(eventId);
            return new Result(true, $"'{item.Name}' deleted");
        }
    }
}
