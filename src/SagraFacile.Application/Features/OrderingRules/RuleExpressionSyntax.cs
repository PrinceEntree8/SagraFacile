using System.Text.RegularExpressions;

namespace SagraFacile.Application.Features.OrderingRules;

public static class RuleExpressionSyntax
{
    public static readonly HashSet<string> AllowedFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "min", "max", "floor", "ceil"
    };

    private static readonly Regex IdentifierPattern = new(
        @"\b(covers|totalItems|[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+)\b",
        RegexOptions.Compiled);

    public static string Normalize(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return string.Empty;
        return IdentifierPattern.Replace(expression, m => $"[{m.Value}]");
    }
}
