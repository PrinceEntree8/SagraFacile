using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Common;
using SagraFacile.Domain.Common;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Features.Orders;

public static class CreateFollowUpOrder
{
    public record Result(int OrderId, int OrderNumber);

    public record Command(
        int ParentOrderId,
        IReadOnlyCollection<CreateDraftOrder.DraftLine> Lines,
        OrderActorDescriptor Actor
    ) : ICommand<CommandResult<Result>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ParentOrderId).GreaterThan(0);
        }
    }

    public class Handler(
        IOrderRepository repository,
        IMenuRepository menuRepository,
        IOrderStateMachine stateMachine,
        IOrderingRulesService rulesService) : ICommandHandler<Command, CommandResult<Result>>
    {
        public async Task<CommandResult<Result>> Handle(Command command, CancellationToken ct)
        {
            var parent = await repository.GetByIdWithLinesAndEventAsync(command.ParentOrderId, ct);
            if (parent is null)
                return new CommandResult<Result>(false, null, "Parent order not found");

            var policy = stateMachine.PolicyFor(parent.Event);
            if (!policy.AllowFollowUpOrders)
                return new CommandResult<Result>(false, null, "Follow-up orders are disabled for this event");

            var menuItems = await menuRepository.GetByEventIdAsync(parent.EventId, includeUnavailable: true, ct);
            var menuDict = menuItems.ToDictionary(m => m.Id);

            const int maxRetries = 5;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var orderNumber = await repository.GetNextOrderNumberAsync(parent.EventId, ct);

                try
                {
                    var followUp = Order.CreateFollowUp(parent, parent.Event, orderNumber, command.Actor.UserId);

                    foreach (var draftLine in command.Lines ?? [])
                    {
                        if (!menuDict.TryGetValue(draftLine.MenuItemId, out var menuItem))
                            return new CommandResult<Result>(false, null, $"Menu item {draftLine.MenuItemId} not found");

                        followUp.AddLine(policy, menuItem.Id, menuItem.Name, menuItem.PriceInCents, draftLine.Quantity, draftLine.Notes);
                    }

                    var linesForValidation = followUp.Lines
                        .Select(l => new OrderValidationLine(l.MenuItemId, l.Quantity))
                        .ToList();
                    var validation = await rulesService.ValidateOrderAsync(
                        new OrderValidationInput(followUp.EventId, followUp.Covers, linesForValidation), ct);

                    if (!validation.IsValid)
                        return new CommandResult<Result>(false, null, string.Join(" ", validation.Errors.Select(e => e.Message)));

                    await repository.AddAsync(followUp, ct);
                    await repository.SaveChangesAsync(ct);

                    return new CommandResult<Result>(true, new Result(followUp.Id, followUp.OrderNumber), $"Follow-up order #{followUp.OrderNumber} created");
                }
                catch (RepositoryUniqueConstraintException)
                {
                    if (attempt == maxRetries - 1)
                        return new CommandResult<Result>(false, null, "Failed to assign unique order number. Please try again.");
                }
                catch (DomainRuleViolationException ex)
                {
                    return new CommandResult<Result>(false, null, ex.Message);
                }
            }

            return new CommandResult<Result>(false, null, "Failed to create follow-up order.");
        }
    }
}
