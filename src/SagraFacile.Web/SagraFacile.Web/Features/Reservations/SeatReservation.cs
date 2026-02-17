using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;

namespace SagraFacile.Web.Features.Reservations;

public static class SeatReservation
{
    public record Command(int ReservationId);

    public record Result(bool Success, string Message);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReservationId)
                .GreaterThan(0).WithMessage("Reservation ID must be greater than 0");
        }
    }

    public static async Task<Result> Handle(Command command, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var reservation = await context.TableReservations
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

        await context.SaveChangesAsync(cancellationToken);

        return new Result(true, $"Reservation {reservation.QueueNumber} seated successfully");
    }
}
