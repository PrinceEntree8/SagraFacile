using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;
using SagraFacile.Web.Infrastructure.CQRS;

namespace SagraFacile.Web.Features.Reservations;

public static class UpdateTableCover
{
    public record Command(int? TableId, string? TableNumber, int CoverCount) : ICommand<Result>;

    public record Result(int Id, string TableNumber, int CoverCount);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x)
                .Must(x => x.TableId.HasValue || !string.IsNullOrEmpty(x.TableNumber))
                .WithMessage("Either TableId or TableNumber must be provided");

            RuleFor(x => x.CoverCount)
                .GreaterThan(0).WithMessage("Cover count must be greater than 0")
                .LessThanOrEqualTo(50).WithMessage("Cover count must not exceed 50");

            RuleFor(x => x.TableNumber)
                .MaximumLength(50).WithMessage("Table number must not exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.TableNumber));
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
            Table? table = null;

            if (command.TableId.HasValue)
            {
                table = await _context.Tables
                    .FirstOrDefaultAsync(t => t.Id == command.TableId.Value, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(command.TableNumber))
            {
                table = await _context.Tables
                    .FirstOrDefaultAsync(t => t.TableNumber == command.TableNumber, cancellationToken);
            }

            if (table == null)
            {
                // Create new table
                table = new Table
                {
                    TableNumber = command.TableNumber ?? $"T{DateTime.UtcNow:yyyyMMddHHmmss}",
                    CoverCount = command.CoverCount,
                    Status = "Available",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Tables.Add(table);
            }
            else
            {
                // Update existing table
                table.CoverCount = command.CoverCount;
                table.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new Result(table.Id, table.TableNumber, table.CoverCount);
        }
    }
}
