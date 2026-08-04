using FluentValidation;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.OrderingRules;

public static class DeleteOrderingRule
{
    public record Command(int Id) : ICommand<Result>;
    public record Result(bool Success, string Message);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }

    public class Handler(IOrderingRuleRepository repository) : ICommandHandler<Command, Result>
    {
        public async Task<Result> Handle(Command command, CancellationToken ct)
        {
            await repository.DeleteAsync(command.Id, ct);
            await repository.SaveChangesAsync(ct);
            return new Result(true, "Rule deleted successfully.");
        }
    }
}
