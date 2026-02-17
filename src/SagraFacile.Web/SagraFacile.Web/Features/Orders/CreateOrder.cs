using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;

namespace SagraFacile.Web.Features.Orders;

public static class CreateOrder
{
    public record Command(string CustomerName, decimal TotalAmount);

    public record Result(int Id, string OrderNumber);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage("Customer name is required")
                .MaximumLength(200).WithMessage("Customer name must not exceed 200 characters");

            RuleFor(x => x.TotalAmount)
                .GreaterThan(0).WithMessage("Total amount must be greater than 0");
        }
    }

    public static async Task<Result> Handle(Command command, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var orderNumber = await GenerateOrderNumberAsync(context, cancellationToken);

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerName = command.CustomerName,
            TotalAmount = command.TotalAmount,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        return new Result(order.Id, order.OrderNumber);
    }

    private static async Task<string> GenerateOrderNumberAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var lastOrder = await context.Orders
            .Where(o => o.OrderNumber.StartsWith(date))
            .OrderByDescending(o => o.OrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = 1;
        if (lastOrder != null && lastOrder.OrderNumber.Length >= 8)
        {
            var lastSequence = lastOrder.OrderNumber.Substring(8);
            if (int.TryParse(lastSequence, out var num))
            {
                sequence = num + 1;
            }
        }

        return $"{date}{sequence:D4}";
    }
}
