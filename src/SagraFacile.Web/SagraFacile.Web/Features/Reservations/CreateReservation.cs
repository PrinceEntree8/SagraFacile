using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;
using SagraFacile.Web.Infrastructure.CQRS;

namespace SagraFacile.Web.Features.Reservations;

public static class CreateReservation
{
    public record Command(string CustomerName, int PartySize, string Notes = "") : ICommand<Result>;

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

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes must not exceed 500 characters");
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
            var queueNumber = await GenerateQueueNumberAsync(cancellationToken);

            var reservation = new TableReservation
            {
                QueueNumber = queueNumber,
                CustomerName = command.CustomerName,
                PartySize = command.PartySize,
                Notes = command.Notes,
                Status = "Waiting",
                CreatedAt = DateTime.UtcNow
            };

            _context.TableReservations.Add(reservation);
            await _context.SaveChangesAsync(cancellationToken);

            return new Result(reservation.Id, reservation.QueueNumber);
        }

        private async Task<string> GenerateQueueNumberAsync(CancellationToken cancellationToken)
        {
            const int DatePrefixLength = 8; // yyyyMMdd format
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var lastReservation = await _context.TableReservations
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
}
