using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using NCalc;

namespace SagraFacile.Application.Features.OrderingRules;

public record RuleExpressionValidationResult(
    bool IsValid,
    string? Error,
    IReadOnlyCollection<string> Variables);

public interface IRuleExpressionValidator
{
    RuleExpressionValidationResult Validate(string rawExpression);
    Expression GetOrParse(string rawExpression);
}

public sealed class RuleExpressionValidator : IRuleExpressionValidator
{
    private static readonly ConcurrentDictionary<string, Expression> _cache = new();
    private static readonly Regex VariablePattern = new(
        @"^(covers|totalItems|(category|item)\.[A-Za-z0-9_-]+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public RuleExpressionValidationResult Validate(string rawExpression)
    {
        if (string.IsNullOrWhiteSpace(rawExpression))
        {
            return new RuleExpressionValidationResult(false, "Expression is required.", Array.Empty<string>());
        }

        try
        {
            var expr = GetOrParse(rawExpression);

            if (expr.HasErrors())
            {
                return new RuleExpressionValidationResult(
                    false,
                    $"Invalid expression syntax: {expr.Error}",
                    Array.Empty<string>());
            }

            // Check function names
            var functionNames = expr.GetFunctionNames();
            var disallowedFunctions = functionNames
                .Where(f => !RuleExpressionSyntax.AllowedFunctions.Contains(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (disallowedFunctions.Count > 0)
            {
                return new RuleExpressionValidationResult(
                    false,
                    $"Disallowed function(s): {string.Join(", ", disallowedFunctions)}",
                    Array.Empty<string>());
            }

            // Check parameter/variable names
            var parameterNames = expr.GetParameterNames();
            var disallowedVariables = parameterNames
                .Where(v => !VariablePattern.IsMatch(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (disallowedVariables.Count > 0)
            {
                return new RuleExpressionValidationResult(
                    false,
                    $"Disallowed identifier(s): {string.Join(", ", disallowedVariables)}",
                    Array.Empty<string>());
            }

            // Dry run evaluation with default 0 parameter values to confirm boolean result type
            var normalized = RuleExpressionSyntax.Normalize(rawExpression);
            var dryRunExpr = new Expression(normalized);
            AttachFunctions(dryRunExpr);
            foreach (var pName in parameterNames)
            {
                dryRunExpr.Parameters[pName] = 0;
            }

            var evalResult = dryRunExpr.Evaluate();
            if (evalResult is not bool)
            {
                return new RuleExpressionValidationResult(
                    false,
                    "Expression must evaluate to a boolean result.",
                    parameterNames);
            }

            return new RuleExpressionValidationResult(true, null, parameterNames);
        }
        catch (Exception ex)
        {
            return new RuleExpressionValidationResult(false, $"Invalid expression: {ex.Message}", Array.Empty<string>());
        }
    }

    public Expression GetOrParse(string rawExpression)
    {
        var normalized = RuleExpressionSyntax.Normalize(rawExpression);
        return _cache.GetOrAdd(normalized, n => new Expression(n));
    }

    public static void AttachFunctions(Expression expr)
    {
        expr.EvaluateFunction += (name, args) =>
        {
            var fn = name.ToLowerInvariant();
            if (fn is "min" or "max")
            {
                if (args.Parameters.Count == 2)
                {
                    var a = Convert.ToDouble(args.Parameters.Evaluate(0));
                    var b = Convert.ToDouble(args.Parameters.Evaluate(1));
                    args.Result = fn == "min" ? Math.Min(a, b) : Math.Max(a, b);
                }
            }
            else if (fn is "floor" or "ceil" or "ceiling")
            {
                if (args.Parameters.Count == 1)
                {
                    var val = Convert.ToDouble(args.Parameters.Evaluate(0));
                    args.Result = fn == "floor" ? Math.Floor(val) : Math.Ceiling(val);
                }
            }
        };
    }
}
