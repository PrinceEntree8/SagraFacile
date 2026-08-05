using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Common;
using SagraFacile.Domain.Common;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Features.Orders;

public static class AdvanceOrderStatus
{
    public record Command(
        int OrderId,
        OrderActorDescriptor Actor,
        OrderStatus? TargetStatus = null,
        string? Reason = null
    ) : ICommand<CommandResult>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OrderId).GreaterThan(0);
            RuleFor(x => x.Reason).MaximumLength(500);
        }
    }

    public class Handler(
        IOrderRepository repository,
        IOrderStateMachine stateMachine) : ICommandHandler<Command, CommandResult>
    {
        public async Task<CommandResult> Handle(Command command, CancellationToken ct)
        {
            var order = await repository.GetByIdWithEventAsync(command.OrderId, ct);
            if (order is null)
                return new CommandResult(false, "Order not found");

            var target = command.TargetStatus ?? OrderStatusRules.NextInChain(order.Status);
            if (target is null)
                return new CommandResult(false, $"Order in status {order.Status} cannot be advanced further");

            var policy = stateMachine.PolicyFor(order.Event);

            try
            {
                order.TransitionTo(target.Value, policy, command.Actor, command.Reason);
                await repository.SaveChangesAsync(ct);
                return new CommandResult(true, $"Order #{order.OrderNumber} status changed to {target.Value}");
            }
            catch (OrderTransitionNotAllowedException ex)
            {
                return new CommandResult(false, ex.Message);
            }
            catch (DomainRuleViolationException ex)
            {
                return new CommandResult(false, ex.Message);
            }
            catch (RepositoryConcurrencyException)
            {
                return new CommandResult(false, "This order was modified by another user. Please refresh and try again.");
            }
        }
    }
}
