namespace SagraFacile.Domain.Common;

public class DomainRuleViolationException : Exception
{
    public DomainRuleViolationException(string message) : base(message) { }
}
