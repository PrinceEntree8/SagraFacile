namespace SagraFacile.Web.Infrastructure.CQRS;

public interface IMediator
{
    Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
    Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken);
}
