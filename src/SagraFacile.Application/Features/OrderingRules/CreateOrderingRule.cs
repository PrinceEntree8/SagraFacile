using FluentValidation;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Features.OrderingRules;

public static class CreateOrderingRule
{
    public record Command(
        int EventId,
        OrderingRuleScope Scope,
        string? TargetCode,
        string Expression,
        string? ErrorMessage,
        string? WarningMessage,
        int Priority = 0,
        bool IsActive = true) : ICommand<Result>;

    public record Result(bool Success, string Message, int Id = 0);

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRuleExpressionValidator expressionValidator)
        {
            RuleFor(x => x.EventId).GreaterThan(0);
            RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Expression).NotEmpty();
            RuleFor(x => x.Expression).Custom((expr, ctx) =>
            {
                if (string.IsNullOrWhiteSpace(expr)) return;
                var res = expressionValidator.Validate(expr);
                if (!res.IsValid)
                {
                    ctx.AddFailure(nameof(Command.Expression), res.Error ?? "Invalid expression.");
                }
            });
            RuleFor(x => x).Custom((cmd, ctx) =>
            {
                if (cmd.Scope is OrderingRuleScope.Category or OrderingRuleScope.Item && string.IsNullOrWhiteSpace(cmd.TargetCode))
                {
                    ctx.AddFailure(nameof(Command.TargetCode), "TargetCode is required for Category/Item scope.");
                }
                if (cmd.Scope == OrderingRuleScope.Order && !string.IsNullOrWhiteSpace(cmd.TargetCode))
                {
                    ctx.AddFailure(nameof(Command.TargetCode), "TargetCode must be empty for Order scope.");
                }
                if (string.IsNullOrWhiteSpace(cmd.ErrorMessage) && string.IsNullOrWhiteSpace(cmd.WarningMessage))
                {
                    ctx.AddFailure("Either ErrorMessage or WarningMessage is required.");
                }
            });
        }
    }

    public class Handler(IOrderingRuleRepository repository) : ICommandHandler<Command, Result>
    {
        public async Task<Result> Handle(Command command, CancellationToken ct)
        {
            var rule = OrderingRule.Create(
                command.EventId,
                command.Scope,
                command.TargetCode,
                command.Expression,
                command.ErrorMessage,
                command.WarningMessage,
                command.Priority,
                command.IsActive);

            await repository.AddAsync(rule, ct);
            await repository.SaveChangesAsync(ct);
            return new Result(true, "Rule created successfully.", rule.Id);
        }
    }
}
