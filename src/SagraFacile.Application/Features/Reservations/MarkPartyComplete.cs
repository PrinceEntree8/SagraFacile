using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Common;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Extensions;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class MarkPartyComplete
{
    public record Command(int ReservationId, string MarkedBy = "System") : ICommand<CommandResult>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReservationId).GreaterThan(0).WithMessage("Reservation ID must be greater than 0");
            RuleFor(x => x.MarkedBy)
                .NotEmpty().WithMessage("MarkedBy is required")
                .MaximumLength(200).WithMessage("MarkedBy must not exceed 200 characters");
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

            if (!reservation.Event.AdditionalOptions.Reservations.PartyCompletion.Enabled)
                return new CommandResult(false, "Party completion is not enabled for this event");

            if (reservation.Status != ReservationStatus.Waiting)
                return new CommandResult(false, "Reservation is not in waiting status");

            var oldStatus = reservation.Status;
            reservation.Status = ReservationStatus.PartyCompleted;

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

            return new CommandResult(true, $"Reservation {reservation.SequenceNumber} marked as party complete");
        }
    }
}