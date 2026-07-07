using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Common;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class CallAndSeatReservation
{
    public record Command(int EventId, int SequenceNumber) : ICommand<CommandResult>;

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
        : ICommandHandler<Command, CommandResult>
    {
        public async Task<CommandResult> Handle(Command command, CancellationToken cancellationToken)
        {
            var reservation = await repository.GetByEventAndSequenceAsync(command.EventId, command.SequenceNumber, cancellationToken);

            if (reservation == null)
                return new CommandResult(false, $"Reservation '{command.SequenceNumber}' not found for this event");

            if (reservation.Status == ReservationStatus.Voided)
                return new CommandResult(false, "Cannot seat a voided reservation");

            if (reservation.Status == ReservationStatus.Seated)
                return new CommandResult(false, "Reservation is already seated");

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
                return new CommandResult(false, "This reservation was modified by another user. Please refresh and try again.");
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

            var counters = (await repository.GetCountersAsync(reservation.EventId, cancellationToken))
                .Select(x => new ReservationCounterDto(x.Status, x.Count, x.TotalPeople))
                .ToList();
            await notifier.EnqueueCountersUpdatedAsync(
                new CountersUpdatedNotification(counters),
                cancellationToken).ConfigureAwait(false);

            return new CommandResult(true, $"Reservation {reservation.SequenceNumber} ({reservation.CustomerName}, party of {reservation.PartySize}) seated successfully");
        }
    }
}
