using FluentValidation;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Menu;

public static class UpdateMenuCategory
{
    public record Command(int Id, string Name, int DisplayOrder) : ICommand<Result>;
    public record Result(bool Success, string Message);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    public class Handler : ICommandHandler<Command, Result>
    {
        private readonly IMenuCategoryRepository _repo;

        public Handler(IMenuCategoryRepository repo) => _repo = repo;

        public async Task<Result> Handle(Command command, CancellationToken ct)
        {
            var cat = await _repo.GetByIdAsync(command.Id, ct);
            if (cat is null) return new Result(false, "Category not found");
            cat.Name = command.Name;
            cat.DisplayOrder = command.DisplayOrder;
            await _repo.SaveChangesAsync(ct);
            return new Result(true, $"Category '{cat.Name}' updated");
        }
    }
}
