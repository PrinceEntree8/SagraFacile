namespace SagraFacile.Web.Infrastructure.CQRS;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for query type {queryType.Name}");
        }

        var handleMethod = handlerType.GetMethod("Handle");
        if (handleMethod == null)
        {
            throw new InvalidOperationException($"Handle method not found on handler for {queryType.Name}");
        }

        var result = await (Task<TResult>)handleMethod.Invoke(handler, new object[] { query, cancellationToken })!;
        return result;
    }

    public async Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for command type {commandType.Name}");
        }

        var handleMethod = handlerType.GetMethod("Handle");
        if (handleMethod == null)
        {
            throw new InvalidOperationException($"Handle method not found on handler for {commandType.Name}");
        }

        var result = await (Task<TResult>)handleMethod.Invoke(handler, new object[] { command, cancellationToken })!;
        return result;
    }
}
