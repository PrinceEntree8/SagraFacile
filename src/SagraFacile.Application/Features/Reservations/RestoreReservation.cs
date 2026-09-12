using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Extensions;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class RestoreReservation
{
    public record Command(int ReservationId) : ICommand<ReservationCommandResponse>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReservationId).GreaterThan(0).WithMessage("Reservation ID must be greater than 0");
        }
    }

    public class Handler(IReservationRepository repository, IReservationNotifier notifier)
        : ICommandHandler<Command, ReservationCommandResponse>
    {
        public async Task<ReservationCommandResponse> Handle(Command command, CancellationToken cancellationToken)
        {
            var reservation = await repository.GetByIdAsync(command.ReservationId, cancellationToken);

            if (reservation == null)
                return new ReservationCommandResponse(false, null, "Reservation not found");

            if (reservation.Status != ReservationStatus.Voided)
                return new ReservationCommandResponse(false, null, "Only voided reservations can be restored");

            reservation.Status = ReservationStatus.Waiting;
            reservation.VoidedAt = null;

            try
            {
                await repository.SaveChangesAsync(cancellationToken);
            }
            catch (RepositoryConcurrencyException)
            {
                return new ReservationCommandResponse(false, null, "This reservation was modified by another user. Please refresh and try again.");
            }

            notifier.EnqueueStatusChangedAsync(new ReservationStatusChangedNotification(
                reservation.Id,
                reservation.SequenceNumber,
                reservation.CustomerName,
                reservation.PartySize,
                NewStatus: ReservationStatus.Waiting,
                OldStatus: ReservationStatus.Voided,
                CallCount: null
            ), cancellationToken).Forget();

            var counters = (await repository.GetCountersAsync(reservation.EventId, cancellationToken))
                .Select(x => new ReservationCounterDto(x.Status, x.Count, x.TotalPeople))
                .ToList();

            notifier.EnqueueCountersUpdatedAsync(
                new CountersUpdatedNotification(counters),
                cancellationToken).Forget();

            return new ReservationCommandResponse(true, ReservationDtoMapper.Map(reservation),
                $"Reservation {reservation.SequenceNumber} restored successfully");
        }
    }
}
