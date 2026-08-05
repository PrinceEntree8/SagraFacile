using FluentValidation;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Common;
using SagraFacile.Domain.Common;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Features.Orders;

public static class CreateDraftOrder
{
    public record DraftLine(int MenuItemId, int Quantity, string? Notes = null);

    public record Result(int OrderId, int OrderNumber);

    public record Command(
        int EventId,
        OrderContext Context,
        int? ContextReferenceId,
        string? ContextLabel,
        int Covers,
        string? CustomerName,
        string? Notes,
        IReadOnlyCollection<DraftLine> Lines,
        OrderActorDescriptor Actor
    ) : ICommand<CommandResult<Result>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EventId).GreaterThan(0);
            RuleFor(x => x.Covers).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CustomerName).MaximumLength(200);
            RuleFor(x => x.Notes).MaximumLength(500);
        }
    }

    public class Handler(
        IOrderRepository repository,
        IEventRepository eventRepository,
        IMenuRepository menuRepository,
        IOrderStateMachine stateMachine) : ICommandHandler<Command, CommandResult<Result>>
    {
        public async Task<CommandResult<Result>> Handle(Command command, CancellationToken ct)
        {
            var @event = await eventRepository.GetByIdAsync(command.EventId, ct);
            if (@event is null)
                return new CommandResult<Result>(false, null, "Event not found");

            var policy = stateMachine.PolicyFor(@event);
            var orderOptions = @event.AdditionalOptions.Orders;
            var coverCharge = orderOptions.CoverChargeEnabled ? orderOptions.DefaultCoverChargeInCents : 0;

            var menuItems = await menuRepository.GetByEventIdAsync(command.EventId, includeUnavailable: true, ct);
            var menuDict = menuItems.ToDictionary(m => m.Id);

            const int maxRetries = 5;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var orderNumber = await repository.GetNextOrderNumberAsync(command.EventId, ct);

                try
                {
                    var order = Order.Create(@event, orderNumber, command.Context, command.ContextReferenceId,
                        command.ContextLabel, command.Covers, coverCharge, command.Actor.UserId);
                    order.CustomerName = command.CustomerName;

                    foreach (var draftLine in command.Lines ?? [])
                    {
                        if (!menuDict.TryGetValue(draftLine.MenuItemId, out var menuItem))
                            return new CommandResult<Result>(false, null, $"Menu item {draftLine.MenuItemId} not found");

                        order.AddLine(policy, menuItem.Id, menuItem.Name, menuItem.PriceInCents, draftLine.Quantity, draftLine.Notes);
                    }

                    await repository.AddAsync(order, ct);
                    await repository.SaveChangesAsync(ct);

                    return new CommandResult<Result>(true, new Result(order.Id, order.OrderNumber), $"Draft order #{order.OrderNumber} created");
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

            return new CommandResult<Result>(false, null, "Failed to create draft order.");
        }
    }
}
