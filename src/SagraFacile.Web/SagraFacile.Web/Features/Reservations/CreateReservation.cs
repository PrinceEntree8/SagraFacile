using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;

namespace SagraFacile.Web.Features.Reservations;

public static class CreateReservation
{
    public record Command(string CustomerName, int PartySize, int Priority = 0);

    public record Result(int Id, string QueueNumber);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage("Customer name is required")
                .MaximumLength(200).WithMessage("Customer name must not exceed 200 characters");

            RuleFor(x => x.PartySize)
                .GreaterThan(0).WithMessage("Party size must be greater than 0")
                .LessThanOrEqualTo(50).WithMessage("Party size must not exceed 50");

            RuleFor(x => x.Priority)
                .GreaterThanOrEqualTo(0).WithMessage("Priority must be 0 or greater");
        }
    }

    public static async Task<Result> Handle(Command command, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var queueNumber = await GenerateQueueNumberAsync(context, cancellationToken);

        var reservation = new TableReservation
        {
            QueueNumber = queueNumber,
            CustomerName = command.CustomerName,
            PartySize = command.PartySize,
            Priority = command.Priority,
            Status = "Waiting",
            CreatedAt = DateTime.UtcNow
        };

        context.TableReservations.Add(reservation);
        await context.SaveChangesAsync(cancellationToken);

        return new Result(reservation.Id, reservation.QueueNumber);
    }

    private static async Task<string> GenerateQueueNumberAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        const int DatePrefixLength = 8; // yyyyMMdd format
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var lastReservation = await context.TableReservations
            .Where(r => r.QueueNumber.StartsWith(date))
            .OrderByDescending(r => r.QueueNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = 1;
        if (lastReservation != null && lastReservation.QueueNumber.Length >= DatePrefixLength)
        {
            var lastSequence = lastReservation.QueueNumber.Substring(DatePrefixLength);
            if (int.TryParse(lastSequence, out var num))
            {
                sequence = num + 1;
            }
        }

        return $"{date}{sequence:D4}";
    }
}
