namespace SagraFacile.Application.Infrastructure.CQRS;

/// <summary>
/// Marker interface for queries that return a result.
/// </summary>
public interface IQuery<out TResult>
{
}
