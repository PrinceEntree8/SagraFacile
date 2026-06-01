using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Application.Features.Events;

public static class UpdateEventAdditionalOptions
{
    public record Command(
        int EventId,
        bool PartyCompletionEnabled,
        int PartyCompletionMinPartySize) : ICommand<Result>;

    public record Result(bool Success, string? Error = null);

    public class Handler : ICommandHandler<Command, Result>
    {
        private readonly IEventRepository _repository;

        public Handler(IEventRepository repository) => _repository = repository;

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            if (command.PartyCompletionMinPartySize < 1)
                return new Result(false, "MinPartySizeInvalid");

            var ev = await _repository.GetByIdAsync(command.EventId, cancellationToken);
            if (ev is null)
                return new Result(false, "EventNotFound");

            ev.AdditionalOptions = new EventAdditionalOptions
            {
                Reservations = new ReservationOptions
                {
                    PartyCompletion = new PartyCompletionOptions
                    {
                        Enabled = command.PartyCompletionEnabled,
                        MinPartySize = command.PartyCompletionMinPartySize
                    }
                }
            };

            await _repository.SaveChangesAsync(cancellationToken);
            return new Result(true);
        }
    }
}
