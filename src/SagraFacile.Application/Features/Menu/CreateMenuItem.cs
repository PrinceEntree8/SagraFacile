using FluentValidation;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Features.Menu;

public static class CreateMenuItem
{
    public record Command(int EventId, string Name, string Description, decimal Price, MenuCategory Category, List<int> AllergenIds, bool IsAvailable = true) : ICommand<Result>;
    public record Result(int Id, string Name);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EventId).GreaterThan(0);
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
            var item = new MenuItem
            {
                EventId = command.EventId,
                Name = command.Name,
                Description = command.Description,
                Price = command.Price,
                Category = command.Category,
                IsAvailable = command.IsAvailable,
                CreatedAt = DateTime.UtcNow,
                MenuItemAllergens = command.AllergenIds.Select(id => new MenuItemAllergen { AllergenId = id }).ToList()
            };
            await _repo.AddAsync(item, ct);
            await _repo.SaveChangesAsync(ct);
            return new Result(item.Id, item.Name);
        }
    }
}
