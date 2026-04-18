using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Menu;

public static class DeleteMenuItem
{
    public record Command(int Id) : ICommand<Result>;
    public record Result(bool Success, string Message);

    public class Handler : ICommandHandler<Command, Result>
    {
        private readonly IMenuRepository _repo;

        public Handler(IMenuRepository repo) => _repo = repo;

        public async Task<Result> Handle(Command command, CancellationToken ct)
        {
            var item = await _repo.GetByIdAsync(command.Id, ct);
            if (item is null) return new Result(false, "Item not found");

            await _repo.DeleteAsync(command.Id, ct);
            await _repo.SaveChangesAsync(ct);
            return new Result(true, $"'{item.Name}' deleted");
        }
    }
}
