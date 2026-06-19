using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Common;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Extensions;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class VoidReservation
{
    public record Command(int ReservationId) : ICommand<CommandResult>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReservationId).GreaterThan(0).WithMessage("Reservation ID must be greater than 0");
        }
    }

    public class Handler(IReservationRepository repository, IReservationNotifier notifier)
        : ICommandHandler<Command, CommandResult>
    {
        public async Task<CommandResult> Handle(Command command, CancellationToken cancellationToken)
        {
            var reservation = await repository.GetByIdAsync(command.ReservationId, cancellationToken);

            if (reservation == null)
                return new CommandResult(false, "Reservation not found");

            if (reservation.Status == ReservationStatus.Voided)
                return new CommandResult(false, "Reservation is already voided");

            if (reservation.Status == ReservationStatus.Seated)
                return new CommandResult(false, "Cannot void a seated reservation");

            var oldStatus = reservation.Status;
            reservation.Status = ReservationStatus.Voided;
            reservation.VoidedAt = DateTime.UtcNow;

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
                NewStatus: ReservationStatus.Voided,
                OldStatus: oldStatus,
                CallCount: null
            ), cancellationToken).Forget();

            return new CommandResult(true, $"Reservation {reservation.SequenceNumber} voided successfully");
        }
    }
}
