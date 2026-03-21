using FluentValidation;
using SagraFacile.Web.Data;
using SagraFacile.Web.Infrastructure.CQRS;

namespace SagraFacile.Web.Features.Events;

public static class CreateEvent
{
    public record Command(string Name, string Description, DateTime Date, string Currency, string CurrencySymbol) : ICommand<Result>;
    public record Result(int Id, string Name);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Date).NotEmpty();
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
            RuleFor(x => x.CurrencySymbol).NotEmpty().MaximumLength(5);
        }
    }

    public class Handler : ICommandHandler<Command, Result>
    {
        private readonly ApplicationDbContext _db;
        public Handler(ApplicationDbContext db) => _db = db;

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var ev = new Event
            {
                Name = command.Name,
                Description = command.Description,
                Date = DateTime.SpecifyKind(command.Date.Date, DateTimeKind.Utc),
                Currency = command.Currency,
                CurrencySymbol = command.CurrencySymbol,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
            _db.Events.Add(ev);
            await _db.SaveChangesAsync(cancellationToken);
            return new Result(ev.Id, ev.Name);
        }
    }
}
