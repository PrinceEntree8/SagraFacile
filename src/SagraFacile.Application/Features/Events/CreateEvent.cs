using FluentValidation;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Application.Features.Events;

public static class CreateEvent
{
    public record Command(string Name, string Description, DateTime Date, string Currency, string CurrencySymbol)
        : ICommand<Result>;
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
        private readonly IEventRepository _repository;

        public Handler(IEventRepository repository) => _repository = repository;

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

            await _repository.AddAsync(ev, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return new Result(ev.Id, ev.Name);
        }
    }
}
