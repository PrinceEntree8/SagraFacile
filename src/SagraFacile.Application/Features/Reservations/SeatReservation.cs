using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class SeatReservation
{
    public record Command(int ReservationId) : ICommand<Result>;
    public record Result(bool Success, string Message);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReservationId).GreaterThan(0).WithMessage("Reservation ID must be greater than 0");
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
            var reservation = await _repository.GetByIdAsync(command.ReservationId, cancellationToken);

            if (reservation == null)
                return new Result(false, "Reservation not found");

            switch (reservation.Status)
            {
                case ReservationStatus.Voided:
                    return new Result(false, "Cannot seat a voided reservation");
                case ReservationStatus.Waiting:
                    return new Result(false, "Cannot seat a waiting reservation");
                case ReservationStatus.Seated:
                    return new Result(false, "Reservation is already seated");
                case ReservationStatus.Called:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reservation), reservation.Status, null);
            }

            reservation.Status = ReservationStatus.Seated;
            reservation.SeatedAt = DateTime.UtcNow;

            try
            {
                await _repository.SaveChangesAsync(cancellationToken);
            }
            catch (RepositoryConcurrencyException)
            {
                return new Result(false, "This reservation was modified by another user. Please refresh and try again.");
            }

            await _notifier.NotifyReservationSeatedAsync(reservation.Id, reservation.SequenceNumber, cancellationToken);

            var counters = await _repository.GetCountersAsync(reservation.EventId, cancellationToken);
            await _notifier.NotifyCountersUpdatedAsync(counters, cancellationToken).ConfigureAwait(false);

            return new Result(true, $"Reservation {reservation.SequenceNumber} seated successfully");
        }
    }
}
