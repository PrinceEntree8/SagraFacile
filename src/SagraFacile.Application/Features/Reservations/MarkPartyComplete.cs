using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class MarkPartyComplete
{
    public record Command(int ReservationId, string MarkedBy = "System") : ICommand<Result>;
    public record Result(bool Success, string Message);

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
        : ICommandHandler<Command, Result>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var reservation = await repository.GetByIdWithEventAsync(command.ReservationId, cancellationToken);

            if (reservation == null)
                return new Result(false, "Reservation not found");

            if (!reservation.Event.AdditionalOptions.Reservations.PartyCompletion.Enabled)
                return new Result(false, "Party completion is not enabled for this event");

            if (reservation.Status != ReservationStatus.Waiting)
                return new Result(false, "Reservation is not in waiting status");

            reservation.Status = ReservationStatus.PartyCompleted;

            try
            {
                await repository.SaveChangesAsync(cancellationToken);
            }
            catch (RepositoryConcurrencyException)
            {
                return new Result(false, "This reservation was modified by another user. Please refresh and try again.");
            }

            await notifier.NotifyReservationPartyCompleteAsync(
                reservation.Id,
                reservation.SequenceNumber,
                reservation.CustomerName,
                reservation.PartySize,
                cancellationToken);

            var counters = await repository.GetCountersAsync(reservation.EventId, cancellationToken);
            await notifier.NotifyCountersUpdatedAsync(counters, cancellationToken).ConfigureAwait(false);

            return new Result(true, $"Reservation {reservation.SequenceNumber} marked as party complete");
        }
    }
}