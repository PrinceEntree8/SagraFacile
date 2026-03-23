using FluentValidation;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

public static class CallReservation
{
    public record Command(int ReservationId, string CalledBy = "Receptionist", string Notes = "")
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

        public Handler(IReservationRepository repository) => _repository = repository;

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var reservation = await _repository.GetByIdAsync(command.ReservationId, cancellationToken);

            if (reservation == null)
                return new Result(false, "Reservation not found");

            if (reservation.Status == "Voided")
                return new Result(false, "Cannot call a voided reservation");

            if (reservation.Status == "Seated")
                return new Result(false, "Reservation is already seated");

            var now = DateTime.UtcNow;

            if (reservation.FirstCalledAt == null)
                reservation.FirstCalledAt = now;

            reservation.LastCalledAt = now;
            reservation.CallCount++;
            reservation.Status = "Called";

            var call = new ReservationCall
            {
                TableReservationId = reservation.Id,
                CalledAt = now,
                CalledBy = command.CalledBy,
                Notes = command.Notes
            };
            await _repository.AddCallAsync(call, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return new Result(true, $"Reservation {reservation.QueueNumber} called successfully (call #{reservation.CallCount})");
        }
    }
}
