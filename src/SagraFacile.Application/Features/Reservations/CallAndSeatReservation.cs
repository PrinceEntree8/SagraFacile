using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class CallAndSeatReservation
{
    public record Command(string QueueNumber) : ICommand<Result>;
    public record Result(bool Success, string Message);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.QueueNumber)
                .NotEmpty().WithMessage("Queue number is required")
                .MaximumLength(50).WithMessage("Queue number must not exceed 50 characters");
        }
    }

    public class Handler(IReservationRepository repository, IReservationNotifier notifier)
        : ICommandHandler<Command, Result>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var reservation = await repository.GetByQueueNumberTodayAsync(command.QueueNumber, cancellationToken);

            if (reservation == null)
                return new Result(false, $"Reservation '{command.QueueNumber}' not found for today");

            if (reservation.Status == "Voided")
                return new Result(false, "Cannot seat a voided reservation");

            if (reservation.Status == "Seated")
                return new Result(false, "Reservation is already seated");

            var now = DateTime.UtcNow;
            bool didCall = reservation.Status != "Called";

            // Add Call event if not yet called
            if (didCall)
            {
                if (reservation.FirstCalledAt == null)
                    reservation.FirstCalledAt = now;

                reservation.LastCalledAt = now;
                reservation.CallCount++;
                reservation.Status = "Called";

                var call = new ReservationCall
                {
                    TableReservationId = reservation.Id,
                    CalledAt = now,
                    CalledBy = "HeadWaiter",
                    Notes = "Manual seat"
                };
                await repository.AddCallAsync(call, cancellationToken);
            }

            // Seat the reservation
            reservation.Status = "Seated";
            reservation.SeatedAt = now;

            try
            {
                await repository.SaveChangesAsync(cancellationToken);
            }
            catch (RepositoryConcurrencyException)
            {
                return new Result(false, "This reservation was modified by another user. Please refresh and try again.");
            }

            if (didCall)
                await notifier.NotifyReservationCalledAsync(
                    reservation.Id,
                    reservation.QueueNumber,
                    reservation.CustomerName,
                    reservation.PartySize,
                    reservation.CallCount,
                    cancellationToken);

            await notifier.NotifyReservationSeatedAsync(reservation.Id, reservation.QueueNumber, cancellationToken);

            var counters = await repository.GetCountersAsync(cancellationToken);
            await notifier.NotifyCountersUpdatedAsync(counters, cancellationToken).ConfigureAwait(false);

            return new Result(true, $"Reservation {reservation.QueueNumber} ({reservation.CustomerName}, party of {reservation.PartySize}) seated successfully");
        }
    }
}
