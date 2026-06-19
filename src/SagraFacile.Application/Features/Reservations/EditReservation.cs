using FluentValidation;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Common;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Extensions;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class EditReservation
{
    public record Command(
        int Id,
        string? CustomerName = null,
        int? PartySize = null,
        string? Notes = null,
        ReservationStatus? Status = null
    ) : ICommand<CommandResult>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0");
            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage("Customer name is required").When(x => x.CustomerName != null)
                .MaximumLength(200).WithMessage("Customer name must not exceed 200 characters").When(x => x.CustomerName != null);
            RuleFor(x => x.PartySize)
                .GreaterThan(0).WithMessage("Party size must be greater than 0").When(x => x.PartySize != null)
                .LessThanOrEqualTo(50).WithMessage("Party size must not exceed 50").When(x => x.PartySize != null);
            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes must not exceed 500 characters")
                .When(x => x.Notes != null);
        }
    }
    
    public class Handler(IReservationRepository repository, IReservationNotifier notifier)
        : ICommandHandler<Command, CommandResult>
    {
        public async Task<CommandResult> Handle(Command command, CancellationToken cancellationToken)
        {
            var reservation = await repository.GetByIdAsync(command.Id, cancellationToken);
            
            if (reservation == null)
            {
                return new CommandResult(false, Message: "Failed to update reservation");
            }
            
            var oldStatus = reservation.Status;

            if (command.PartySize.HasValue)
            {
                reservation.PartySize = command.PartySize.Value;
            }

            if (command.Status.HasValue)
            {
                reservation.Status = command.Status.Value;
            }

            if (command.Notes != null)
            {
                reservation.Notes = command.Notes;
            }

            if (command.CustomerName != null)
            {
                reservation.CustomerName = command.CustomerName;
            }
            
            await repository.SaveChangesAsync(cancellationToken);
            
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
            
            return new CommandResult(true, $"Reservation {reservation.Id} has been updated");
        }
    }
}