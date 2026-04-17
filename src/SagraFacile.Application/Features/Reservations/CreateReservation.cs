using FluentValidation;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Features.Reservations;

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
        private const int SequencePadding = 4;
        private readonly IReservationRepository _repository;
        private readonly IReservationNotifier _notifier;

        public Handler(IReservationRepository repository, IReservationNotifier notifier)
        {
            _repository = repository;
            _notifier = notifier;
        }

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var queueNumber = await GenerateQueueNumberAsync(date, cancellationToken);

            var reservation = new TableReservation
            {
                Date = date,
                QueueNumber = queueNumber,
                CustomerName = command.CustomerName,
                PartySize = command.PartySize,
                Notes = command.Notes,
                Status = "Waiting",
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(reservation, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            await _notifier.NotifyReservationCreatedAsync(
                reservation.Id,
                reservation.QueueNumber,
                reservation.CustomerName,
                reservation.PartySize,
                cancellationToken);

            return new Result(reservation.Id, reservation.QueueNumber);
        }

        private async Task<string> GenerateQueueNumberAsync(string date, CancellationToken cancellationToken)
        {
            var last = await _repository.GetLastByDatePrefixAsync(date, cancellationToken);

            var sequence = 1;
            if (last != null && int.TryParse(last.QueueNumber, out var num))
                sequence = num + 1;

            return sequence.ToString($"D{SequencePadding}");
        }
    }
}
