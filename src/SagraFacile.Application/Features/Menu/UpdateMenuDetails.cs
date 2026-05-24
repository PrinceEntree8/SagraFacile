using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Features.Menu;

public static class UpdateMenuDetails
{
    public record Command(int EventId, string? WarningMessage, string? Header, string? Footer) : ICommand<Result>;
    public record Result(bool Success);

    public class Handler(IMenuDetailsRepository repo, IMenuCacheService cache) : ICommandHandler<Command, Result>
    {
        public async Task<Result> Handle(Command command, CancellationToken ct)
        {
            var existing = await repo.GetByEventIdAsync(command.EventId, ct);
            if (existing is null)
            {
                existing = new MenuDetails { EventId = command.EventId };
            }
            existing.WarningMessage = command.WarningMessage;
            existing.Header = command.Header;
            existing.Footer = command.Footer;
            await repo.UpsertAsync(existing, ct);
            await repo.SaveChangesAsync(ct);
            cache.InvalidateMenu(command.EventId);
            return new Result(true);
        }
    }
}
