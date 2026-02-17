using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;

namespace SagraFacile.Web.Features.Reservations;

public static class CallReservation
{
    public record Command(int ReservationId, string CalledBy = "Receptionist", string Notes = "");

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
            return new Result(false, "Cannot call a voided reservation");
        }

        if (reservation.Status == "Seated")
        {
            return new Result(false, "Reservation is already seated");
        }

        var now = DateTime.UtcNow;
        
        // Update reservation
        if (reservation.FirstCalledAt == null)
        {
            reservation.FirstCalledAt = now;
        }
        reservation.LastCalledAt = now;
        reservation.CallCount++;
        reservation.Status = "Called";

        // Add call log
        var call = new ReservationCall
        {
            TableReservationId = reservation.Id,
            CalledAt = now,
            CalledBy = command.CalledBy,
            Notes = command.Notes
        };
        context.ReservationCalls.Add(call);

        await context.SaveChangesAsync(cancellationToken);

        return new Result(true, $"Reservation {reservation.QueueNumber} called successfully (call #{reservation.CallCount})");
    }
}
