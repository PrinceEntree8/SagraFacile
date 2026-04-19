using FluentValidation;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Features.Menu;

public static class CreateMenuCategory
{
    public record Command(string Name, int DisplayOrder = 0) : ICommand<Result>;
    public record Result(int Id, string Name);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    public class Handler : ICommandHandler<Command, Result>
    {
        private readonly IMenuCategoryRepository _repo;

        public Handler(IMenuCategoryRepository repo) => _repo = repo;

        public async Task<Result> Handle(Command command, CancellationToken ct)
        {
            var cat = new MenuCategory
            {
                Name = command.Name,
                DisplayOrder = command.DisplayOrder
            };
            await _repo.AddAsync(cat, ct);
            await _repo.SaveChangesAsync(ct);
            return new Result(cat.Id, cat.Name);
        }
    }
}
