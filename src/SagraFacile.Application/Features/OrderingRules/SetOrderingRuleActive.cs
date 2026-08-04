using FluentValidation;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.OrderingRules;

public static class SetOrderingRuleActive
{
    public record Command(int Id, bool IsActive) : ICommand<Result>;
    public record Result(bool Success, string Message);

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IOrderingRuleRepository repository, IRuleExpressionValidator expressionValidator)
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x).CustomAsync(async (cmd, ctx, ct) =>
            {
                if (!cmd.IsActive) return; // Deactivating is always allowed
                var rule = await repository.GetByIdAsync(cmd.Id, ct);
                if (rule is null) return;

                var res = expressionValidator.Validate(rule.Expression);
                if (!res.IsValid)
                {
                    ctx.AddFailure(nameof(Command.IsActive), $"Cannot activate rule with invalid expression: {res.Error}");
                }
            });
        }
    }

    public class Handler(IOrderingRuleRepository repository) : ICommandHandler<Command, Result>
    {
        public async Task<Result> Handle(Command command, CancellationToken ct)
        {
            var rule = await repository.GetByIdAsync(command.Id, ct);
            if (rule is null) return new Result(false, "Rule not found.");

            rule.SetActive(command.IsActive);
            await repository.UpdateAsync(rule, ct);
            await repository.SaveChangesAsync(ct);
            return new Result(true, $"Rule active status set to {command.IsActive}.");
        }
    }
}
