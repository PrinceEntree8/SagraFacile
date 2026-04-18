using FluentValidation;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Features.Menu;

public static class UpdateMenuItem
{
    public record Command(int Id, string Name, string Description, decimal Price, MenuCategory Category, List<int> AllergenIds, bool IsAvailable) : ICommand<Result>;
    public record Result(bool Success, string Message);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        }
    }

    public class Handler : ICommandHandler<Command, Result>
    {
        private readonly IMenuRepository _repo;

        public Handler(IMenuRepository repo) => _repo = repo;

        public async Task<Result> Handle(Command command, CancellationToken ct)
        {
            var item = await _repo.GetByIdAsync(command.Id, ct);
            if (item is null) return new Result(false, "Item not found");

            item.Name = command.Name;
            item.Description = command.Description;
            item.Price = command.Price;
            item.Category = command.Category;
            item.IsAvailable = command.IsAvailable;
            item.UpdatedAt = DateTime.UtcNow;
            item.MenuItemAllergens.Clear();
            foreach (var aid in command.AllergenIds)
                item.MenuItemAllergens.Add(new MenuItemAllergen { MenuItemId = item.Id, AllergenId = aid });

            await _repo.SaveChangesAsync(ct);
            return new Result(true, $"'{item.Name}' updated");
        }
    }
}
