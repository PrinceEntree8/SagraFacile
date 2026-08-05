namespace SagraFacile.Domain.Features.Orders;

public enum OrderActor { Customer, Cashier, Kitchen, Supervisor, Admin, System }

public readonly record struct OrderActorDescriptor(string? UserId, OrderActor Role)
{
    public static OrderActorDescriptor Customer(string? userId = null) => new(userId, OrderActor.Customer);
    public static OrderActorDescriptor System() => new(null, OrderActor.System);
}
