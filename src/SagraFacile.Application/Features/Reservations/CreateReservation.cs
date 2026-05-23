using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class CreateReservation
{
    public record Command(int EventId, string CustomerName, int PartySize, string? Notes = null) : ICommand<Result>;
    public record Result(int Id, int SequenceNumber);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EventId)
                .GreaterThan(0).WithMessage("EventId must be greater than 0");
            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage("Customer name is required")
                .MaximumLength(200).WithMessage("Customer name must not exceed 200 characters");
            RuleFor(x => x.PartySize)
                .GreaterThan(0).WithMessage("Party size must be greater than 0")
                .LessThanOrEqualTo(50).WithMessage("Party size must not exceed 50");
            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes must not exceed 500 characters")
                .When(x => x.Notes != null);
        }
    }

    public class Handler : ICommandHandler<Command, Result>
    {
        private readonly IReservationRepository _repository;
        private readonly IReservationNotifier _notifier;

        public Handler(IReservationRepository repository, IReservationNotifier notifier)
        {
            _repository = repository;
            _notifier = notifier;
        }

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            const int maxRetries = 5;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var sequenceNumber = await _repository.GetNextSequenceNumberAsync(command.EventId, cancellationToken);
                var reservation = new Reservation
                {
                    EventId        = command.EventId,
                    SequenceNumber = sequenceNumber,
                    CustomerName   = command.CustomerName,
                    PartySize      = command.PartySize,
                    Notes          = command.Notes,
                    Status         = ReservationStatus.Waiting,
                    CreatedAt      = DateTime.UtcNow
                };

                try
                {
                    await _repository.AddAsync(reservation, cancellationToken);
                    await _repository.SaveChangesAsync(cancellationToken);

                    await _notifier.NotifyReservationCreatedAsync(
                        reservation.Id,
                        reservation.SequenceNumber,
                        reservation.CustomerName,
                        reservation.PartySize,
                        cancellationToken);

                    var counters = await _repository.GetCountersAsync(command.EventId, cancellationToken);
                    await _notifier.NotifyCountersUpdatedAsync(counters, cancellationToken).ConfigureAwait(false);

                    return new Result(reservation.Id, reservation.SequenceNumber);
                }
                catch (RepositoryUniqueConstraintException)
                {
                    if (attempt == maxRetries - 1) throw;
                }
            }

            throw new InvalidOperationException("Failed to create reservation after maximum retries.");
        }
    }
}
