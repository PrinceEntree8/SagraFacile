namespace SagraFacile.Application.Exceptions;

/// <summary>
/// Thrown by a repository when a concurrent write conflict is detected
/// (e.g. another user has already modified the same record).
/// Handlers should catch this and return a user-friendly failure result.
/// </summary>
public class RepositoryConcurrencyException : Exception
{
    public RepositoryConcurrencyException()
        : base("The record was modified by another user. Please refresh and try again.") { }
}
