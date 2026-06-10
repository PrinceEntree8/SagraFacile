using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class CallReservation
{
    public record Command(int ReservationId, string CalledBy = "Receptionist", string? Notes = null)
        : ICommand<Result>;
    public record Result(bool Success, string Message);

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
            var reservation = await _repository.GetByIdWithEventAsync(command.ReservationId, cancellationToken);

            if (reservation == null)
                return new Result(false, "Reservation not found");

            if (reservation.Status == ReservationStatus.Voided)
                return new Result(false, "Cannot call a voided reservation");

            if (reservation.Status == ReservationStatus.Seated)
                return new Result(false, "Reservation is already seated");

            var partyCompletionEnabled = reservation.Event.AdditionalOptions.Reservations.PartyCompletion.Enabled;

            if (partyCompletionEnabled)
            {
                if (reservation.Status == ReservationStatus.Waiting)
                    return new Result(false, "Mark party complete first");

                if (reservation.Status != ReservationStatus.PartyCompleted && reservation.Status != ReservationStatus.Called)
                    return new Result(false, "Reservation cannot be called from its current status");
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
            await _repository.AddCallAsync(call, cancellationToken);

            try
            {
                await _repository.SaveChangesAsync(cancellationToken);
            }
            catch (RepositoryConcurrencyException)
            {
                return new Result(false, "This reservation was modified by another user. Please refresh and try again.");
            }

            await _notifier.EnqueueStatusChangedAsync(new ReservationStatusChangedNotification(
                reservation.Id,
                reservation.SequenceNumber,
                reservation.CustomerName,
                reservation.PartySize,
                NewStatus: ReservationStatus.Called,
                OldStatus: oldStatus,
                CallCount: reservation.CallCount
            ), cancellationToken);

            var counters = await _repository.GetCountersAsync(reservation.EventId, cancellationToken);
            await _notifier.EnqueueCountersUpdatedAsync(
                new CountersUpdatedNotification(counters),
                cancellationToken).ConfigureAwait(false);

            return new Result(true, $"Reservation {reservation.SequenceNumber} called successfully (call #{reservation.CallCount})");
        }
    }
}
