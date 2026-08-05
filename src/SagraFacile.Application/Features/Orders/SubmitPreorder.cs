using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Common;
using SagraFacile.Domain.Common;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Features.Orders;

public static class SubmitPreorder
{
    public record Command(
        int OrderId,
        OrderActorDescriptor Actor
    ) : ICommand<CommandResult>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OrderId).GreaterThan(0);
        }
    }

    public class Handler(
        IOrderRepository repository,
        IOrderStateMachine stateMachine,
        IOrderingRulesService rulesService) : ICommandHandler<Command, CommandResult>
    {
        public async Task<CommandResult> Handle(Command command, CancellationToken ct)
        {
            var order = await repository.GetByIdWithLinesAndEventAsync(command.OrderId, ct);
            if (order is null)
                return new CommandResult(false, "Order not found");

            var policy = stateMachine.PolicyFor(order.Event);

            var lines = order.Lines
                .Select(l => new OrderValidationLine(l.MenuItemId, l.Quantity))
                .ToList();
            var validation = await rulesService.ValidateOrderAsync(
                new OrderValidationInput(order.EventId, order.Covers, lines), ct);

            if (!validation.IsValid)
                return new CommandResult(false, string.Join(" ", validation.Errors.Select(e => e.Message)));

            try
            {
                order.TransitionTo(OrderStatus.Preorder, policy, command.Actor);

                if (policy.AutoConfirmPreorders && policy.CanTransition(OrderStatus.Preorder, OrderStatus.Confirmed))
                {
                    order.TransitionTo(OrderStatus.Confirmed, policy, OrderActorDescriptor.System());
                }

                await repository.SaveChangesAsync(ct);
                return new CommandResult(true, $"Preorder #{order.OrderNumber} submitted successfully");
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
