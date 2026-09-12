using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Extensions;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class SeatReservation
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

            switch (reservation.Status)
            {
                case ReservationStatus.Voided:
                    return new ReservationCommandResponse(false, null, "Cannot seat a voided reservation");
                case ReservationStatus.Waiting:
                    return new ReservationCommandResponse(false, null, "Cannot seat a waiting reservation");
                case ReservationStatus.Seated:
                    return new ReservationCommandResponse(false, null, "Reservation is already seated");
                case ReservationStatus.Called:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reservation), reservation.Status, null);
            }

            const ReservationStatus oldStatus = ReservationStatus.Called;
            reservation.Status = ReservationStatus.Seated;
            reservation.SeatedAt = DateTime.UtcNow;

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

            return new ReservationCommandResponse(true, ReservationDtoMapper.Map(reservation),
                $"Reservation {reservation.SequenceNumber} seated successfully");
        }
    }
}
