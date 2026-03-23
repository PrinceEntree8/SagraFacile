using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Application.Features.Events;

public static class ActivateEvent
{
    public record Command(int EventId) : ICommand<Result>;
    public record Result(bool Success, string Message);

    public class Handler : ICommandHandler<Command, Result>
    {
        private readonly IEventRepository _repository;

        public Handler(IEventRepository repository) => _repository = repository;

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var ev = await _repository.GetByIdAsync(command.EventId, cancellationToken);
            if (ev == null)
                return new Result(false, "Event not found");

            await _repository.DeactivateAllAsync(cancellationToken);
            ev.IsActive = true;
            await _repository.SaveChangesAsync(cancellationToken);

            return new Result(true, $"Event '{ev.Name}' activated");
        }
    }
}
