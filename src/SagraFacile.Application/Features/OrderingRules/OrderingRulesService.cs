using NCalc;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Features.OrderingRules;

public sealed class OrderingRulesService(
    IOrderingRuleRepository ruleRepository,
    IRuleEvaluationContextBuilder contextBuilder) : IOrderingRulesService
{
    public async Task<OrderValidationResult> ValidateOrderAsync(OrderValidationInput input, CancellationToken ct = default)
    {
        var rules = await ruleRepository.GetActiveByEventAsync(input.EventId, ct);
        var orderedRules = rules.OrderBy(r => r.Priority).ThenBy(r => r.Id);
        var context = await contextBuilder.BuildAsync(input, ct);

        var errors = new List<RuleViolation>();
        var warnings = new List<RuleViolation>();

        foreach (var rule in orderedRules)
        {
            ct.ThrowIfCancellationRequested();

            if (rule.Scope == OrderingRuleScope.Category &&
                (string.IsNullOrWhiteSpace(rule.TargetCode) || !context.PresentCategoryCodes.Contains(rule.TargetCode)))
            {
                continue;
            }

            if (rule.Scope == OrderingRuleScope.Item &&
                (string.IsNullOrWhiteSpace(rule.TargetCode) || !context.PresentItemCodes.Contains(rule.TargetCode)))
            {
                continue;
            }

            bool? satisfied = Evaluate(rule, context, out var diagnosticMessage);

            if (satisfied == true)
            {
                continue;
            }

            var message = diagnosticMessage ?? rule.Message;
            var violation = new RuleViolation(
                rule.Id,
                rule.Scope,
                rule.TargetCode,
                rule.Expression,
                message,
                rule.IsBlocking);

            if (rule.IsBlocking)
            {
                errors.Add(violation);
            }
            else
            {
                warnings.Add(violation);
            }
        }

        return new OrderValidationResult(errors.Count == 0, errors, warnings);
    }

    private bool? Evaluate(OrderingRule rule, RuleEvaluationContext context, out string? diagnosticMessage)
    {
        diagnosticMessage = null;
        try
        {
            var normalized = RuleExpressionSyntax.Normalize(rule.Expression);
            var evalExpr = new Expression(normalized);
            RuleExpressionValidator.AttachFunctions(evalExpr);

            evalExpr.Parameters["covers"] = context.Covers;
            evalExpr.Parameters["totalItems"] = context.TotalItems;

            evalExpr.EvaluateParameter += (name, args) =>
            {
                if (name.StartsWith("category.", StringComparison.OrdinalIgnoreCase))
                {
                    var code = name[9..];
                    args.Result = context.CategoryQuantities.GetValueOrDefault(code, 0);
                }
                else if (name.StartsWith("item.", StringComparison.OrdinalIgnoreCase))
                {
                    var code = name[5..];
                    args.Result = context.ItemQuantities.GetValueOrDefault(code, 0);
                }
            };

            var evalResult = evalExpr.Evaluate();
            if (evalResult is bool boolResult)
            {
                return boolResult;
            }

            diagnosticMessage = $"Rule #{rule.Id} evaluated to a non-boolean result: {evalResult}";
            return false;
        }
        catch (Exception ex)
        {
            diagnosticMessage = $"Rule #{rule.Id} failed to evaluate: {ex.Message}";
            return false;
        }
    }
}
