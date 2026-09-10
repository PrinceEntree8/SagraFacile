using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Extensions;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class CreateReservation
{
    public record Command(
        int EventId,
        string CustomerName,
        int PartySize,
        string? Notes = null,
        bool PartyComplete = false
    ) : ICommand<ReservationCommandResponse>;

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

    public class Handler(IReservationRepository repository, IReservationNotifier notifier, IEventRepository eventRepository)
        : ICommandHandler<Command, ReservationCommandResponse>
    {

        public async Task<ReservationCommandResponse> Handle(Command command, CancellationToken cancellationToken)
        {
            var reservationEvent = await eventRepository.GetByIdAsync(command.EventId, cancellationToken);
            var partyCompletionEnabled = reservationEvent?.AdditionalOptions.Reservations.PartyCompletion.Enabled ?? false;
            var minPartySize = reservationEvent?.AdditionalOptions.Reservations.PartyCompletion.MinPartySize ?? 1;

            var partyComplete = command.PartyComplete;
            if (partyCompletionEnabled && !command.PartyComplete)
                partyComplete = command.PartySize < minPartySize;

            const int maxRetries = 5;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                await repository.AcquireEventLockAsync(command.EventId, cancellationToken);
                var sequenceNumber = await repository.GetNextSequenceNumberAsync(command.EventId, cancellationToken);
                var reservation = new Reservation
                {
                    EventId        = command.EventId,
                    SequenceNumber = sequenceNumber,
                    CustomerName   = command.CustomerName,
                    PartySize      = command.PartySize,
                    Notes          = command.Notes,
                    Status         = partyComplete && partyCompletionEnabled ? ReservationStatus.PartyCompleted : ReservationStatus.Waiting,
                    CreatedAt      = DateTime.UtcNow
                };

                    await repository.AddAsync(reservation, cancellationToken);
                    try
                    {
                        await repository.SaveChangesAsync(cancellationToken);
                    }
                    catch (RepositoryUniqueConstraintException) when (attempt < maxRetries - 1)
                    {
                        continue;
                    }

                    notifier.EnqueueStatusChangedAsync(new ReservationStatusChangedNotification(
                        reservation.Id,
                        reservation.SequenceNumber,
                        reservation.CustomerName,
                        reservation.PartySize,
                        NewStatus: reservation.Status,
                        OldStatus: null,
                        CallCount: reservation.CallCount
                    ), cancellationToken).Forget();

                    var counters = (await repository.GetCountersAsync(reservation.EventId, cancellationToken))
                        .Select(x => new ReservationCounterDto(x.Status, x.Count, x.TotalPeople))
                        .ToList();
            
                    notifier.EnqueueCountersUpdatedAsync(
                        new CountersUpdatedNotification(counters),
                        cancellationToken).Forget();

                    return new ReservationCommandResponse(true, ReservationDtoMapper.Map(reservation));

            }

            return new ReservationCommandResponse(false, null,
                "Failed to create reservation after maximum retries.");
        }
    }
}
