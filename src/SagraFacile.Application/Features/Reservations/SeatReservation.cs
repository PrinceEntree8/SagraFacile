using FluentValidation;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

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

        public Handler(IReservationRepository repository) => _repository = repository;

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var reservation = await _repository.GetByIdAsync(command.ReservationId, cancellationToken);

            if (reservation == null)
                return new Result(false, "Reservation not found");

            if (reservation.Status == "Voided")
                return new Result(false, "Cannot seat a voided reservation");

            if (reservation.Status == "Seated")
                return new Result(false, "Reservation is already seated");

            reservation.Status = "Seated";
            reservation.SeatedAt = DateTime.UtcNow;

            await _repository.SaveChangesAsync(cancellationToken);

            return new Result(true, $"Reservation {reservation.QueueNumber} seated successfully");
        }
    }
}
