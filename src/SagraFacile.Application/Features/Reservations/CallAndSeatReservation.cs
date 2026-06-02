using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class CallAndSeatReservation
{
    public record Command(int EventId, int SequenceNumber) : ICommand<Result>;
    public record Result(bool Success, string Message);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EventId)
                .GreaterThan(0).WithMessage("EventId must be greater than 0");
            RuleFor(x => x.SequenceNumber)
                .GreaterThan(0).WithMessage("SequenceNumber must be greater than 0");
        }
    }

    public class Handler(IReservationRepository repository, IReservationNotifier notifier)
        : ICommandHandler<Command, Result>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var reservation = await repository.GetByEventAndSequenceAsync(command.EventId, command.SequenceNumber, cancellationToken);

            if (reservation == null)
                return new Result(false, $"Reservation '{command.SequenceNumber}' not found for this event");

            if (reservation.Status == ReservationStatus.Voided)
                return new Result(false, "Cannot seat a voided reservation");

            if (reservation.Status == ReservationStatus.Seated)
                return new Result(false, "Reservation is already seated");

            var oldStatus = reservation.Status;
            var now = DateTime.UtcNow;
            var didCall = reservation.Status != ReservationStatus.Called;

            // Add Call event if not yet called
            if (didCall)
            {
                if (reservation.FirstCalledAt == null)
                    reservation.FirstCalledAt = now;

                reservation.LastCalledAt = now;
                reservation.CallCount++;
                reservation.Status = ReservationStatus.Called;

                var call = new ReservationCall
                {
                    ReservationId = reservation.Id,
                    CalledAt = now,
                    CalledBy = "HeadWaiter",
                    Notes = "Manual seat"
                };
                await repository.AddCallAsync(call, cancellationToken);
            }

            // Seat the reservation
            reservation.Status = ReservationStatus.Seated;
            reservation.SeatedAt = now;

            try
            {
                await repository.SaveChangesAsync(cancellationToken);
            }
            catch (RepositoryConcurrencyException)
            {
                return new Result(false, "This reservation was modified by another user. Please refresh and try again.");
            }

            await notifier.EnqueueStatusChangedAsync(new ReservationStatusChangedNotification(
                reservation.Id,
                reservation.SequenceNumber,
                reservation.CustomerName,
                reservation.PartySize,
                NewStatus: ReservationStatus.Seated,
                OldStatus: oldStatus,
                CallCount: null
            ), cancellationToken);

            var counters = await repository.GetCountersAsync(reservation.EventId, cancellationToken);
            await notifier.EnqueueCountersUpdatedAsync(
                new CountersUpdatedNotification(counters),
                cancellationToken).ConfigureAwait(false);

            return new Result(true, $"Reservation {reservation.SequenceNumber} ({reservation.CustomerName}, party of {reservation.PartySize}) seated successfully");
        }
    }
}
