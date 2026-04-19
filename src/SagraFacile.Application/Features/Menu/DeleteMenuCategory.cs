using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Menu;

public static class DeleteMenuCategory
{
    public record Command(int Id) : ICommand<Result>;
    public record Result(bool Success, string Message);

    public class Handler : ICommandHandler<Command, Result>
    {
        private readonly IMenuCategoryRepository _repo;

        public Handler(IMenuCategoryRepository repo) => _repo = repo;

        public async Task<Result> Handle(Command command, CancellationToken ct)
        {
            var cat = await _repo.GetByIdAsync(command.Id, ct);
            if (cat is null) return new Result(false, "Category not found");
            await _repo.DeleteAsync(command.Id, ct);
            await _repo.SaveChangesAsync(ct);
            return new Result(true, $"Category '{cat.Name}' deleted");
        }
    }
}
