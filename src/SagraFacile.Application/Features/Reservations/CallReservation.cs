using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Common;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Extensions;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class CallReservation
{
    public record Command(int ReservationId, string CalledBy = "Receptionist", string? Notes = null)
        : ICommand<CommandResult>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReservationId)
                .GreaterThan(0).WithMessage("Reservation ID must be greater than 0");
            RuleFor(x => x.CalledBy)
                .NotEmpty().WithMessage("CalledBy is required")
                .MaximumLength(200).WithMessage("CalledBy must not exceed 200 characters");
            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes must not exceed 500 characters");
        }
    }

    public class Handler(IReservationRepository repository, IReservationNotifier notifier)
        : ICommandHandler<Command, CommandResult>
    {

        public async Task<CommandResult> Handle(Command command, CancellationToken cancellationToken)
        {
            var reservation = await repository.GetByIdWithEventAsync(command.ReservationId, cancellationToken);

            if (reservation == null)
                return new CommandResult(false, "Reservation not found");

            if (reservation.Status == ReservationStatus.Voided)
                return new CommandResult(false, "Cannot call a voided reservation");

            if (reservation.Status == ReservationStatus.Seated)
                return new CommandResult(false, "Reservation is already seated");

            var partyCompletionEnabled = reservation.Event.AdditionalOptions.Reservations.PartyCompletion.Enabled;

            if (partyCompletionEnabled)
            {
                if (reservation.Status == ReservationStatus.Waiting)
                    return new CommandResult(false, "Mark party complete first");

                if (reservation.Status != ReservationStatus.PartyCompleted && reservation.Status != ReservationStatus.Called)
                    return new CommandResult(false, "Reservation cannot be called from its current status");
            }

            var now = DateTime.UtcNow;
            var oldStatus = reservation.Status;

            if (reservation.FirstCalledAt == null)
                reservation.FirstCalledAt = now;

            reservation.LastCalledAt = now;
            reservation.CallCount++;
            reservation.Status = ReservationStatus.Called;

            var call = new ReservationCall
            {
                ReservationId = reservation.Id,
                CalledAt = now,
                CalledBy = command.CalledBy,
                Notes = command.Notes
            };
            await repository.AddCallAsync(call, cancellationToken);

            try
            {
                await repository.SaveChangesAsync(cancellationToken);
            }
            catch (RepositoryConcurrencyException)
            {
                return new CommandResult(false, "This reservation was modified by another user. Please refresh and try again.");
            }
            
            notifier.EnqueueStatusChangedAsync(new ReservationStatusChangedNotification(
                reservation.Id,
                reservation.SequenceNumber,
                reservation.CustomerName,
                reservation.PartySize,
                NewStatus: reservation.Status,
                OldStatus: oldStatus,
                CallCount: reservation.CallCount
            ), cancellationToken).Forget();

            var counters = (await repository.GetCountersAsync(reservation.EventId, cancellationToken))
                .Select(x => new ReservationCounterDto(x.Status, x.Count, x.TotalPeople))
                .ToList();
            
            notifier.EnqueueCountersUpdatedAsync(
                new CountersUpdatedNotification(counters),
                cancellationToken).Forget();

            return new CommandResult(true, $"Reservation {reservation.SequenceNumber} called successfully (call #{reservation.CallCount})");
        }
    }
}
