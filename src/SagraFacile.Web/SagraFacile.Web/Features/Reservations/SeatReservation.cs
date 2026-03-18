using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;
using SagraFacile.Web.Infrastructure.CQRS;

namespace SagraFacile.Web.Features.Reservations;

public static class SeatReservation
{
    public record Command(int ReservationId) : ICommand<Result>;

    public record Result(bool Success, string Message);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReservationId)
                .GreaterThan(0).WithMessage("Reservation ID must be greater than 0");
        }
    }

    public class Handler : ICommandHandler<Command, Result>
    {
        private readonly ApplicationDbContext _context;

        public Handler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var reservation = await _context.TableReservations
                .FirstOrDefaultAsync(r => r.Id == command.ReservationId, cancellationToken);

            if (reservation == null)
            {
                return new Result(false, "Reservation not found");
            }

            if (reservation.Status == "Voided")
            {
                return new Result(false, "Cannot seat a voided reservation");
            }

            if (reservation.Status == "Seated")
            {
                return new Result(false, "Reservation is already seated");
            }

            reservation.Status = "Seated";
            reservation.SeatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new Result(true, $"Reservation {reservation.QueueNumber} seated successfully");
        }
    }
}
