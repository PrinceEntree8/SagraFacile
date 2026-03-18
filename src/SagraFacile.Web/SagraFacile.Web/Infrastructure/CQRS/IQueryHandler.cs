namespace SagraFacile.Web.Infrastructure.CQRS;

/// <summary>
/// Handler for queries that return a result
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
}
