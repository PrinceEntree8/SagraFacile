namespace SagraFacile.Application.Infrastructure.CQRS;

/// <summary>
/// Marker interface for commands that return a result.
/// </summary>
public interface ICommand<out TResult>
{
}
