using SagraFacile.Domain.Common;

namespace SagraFacile.Domain.Features.Orders;

public static class OrderContextRules
{
    private static bool RequiresReference(OrderContext context)
        => context is OrderContext.Table or OrderContext.Reservation;

    public static void ValidateReference(OrderContext context, int? contextReferenceId)
    {
        if (RequiresReference(context))
        {
            if (contextReferenceId is null or <= 0)
                throw new DomainRuleViolationException(
                    $"Order context {context} requires a positive ContextReferenceId.");
        }
        else if (contextReferenceId is not null)
        {
            throw new DomainRuleViolationException(
                $"Order context {context} must not carry a ContextReferenceId.");
        }
    }
}
