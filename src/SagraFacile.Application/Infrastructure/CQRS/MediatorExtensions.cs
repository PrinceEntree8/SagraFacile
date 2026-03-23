namespace SagraFacile.Application.Infrastructure.CQRS;

public static class MediatorExtensions
{
    public static Task<TResult> SendAsync<TResult>(
        this IMediator mediator,
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
        => mediator.Send(command, cancellationToken);

    public static Task<TResult> QueryAsync<TResult>(
        this IMediator mediator,
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
        => mediator.Send(query, cancellationToken);
}
