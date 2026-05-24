namespace SagraFacile.Application.Exceptions;

public class RepositoryUniqueConstraintException : Exception
{
    public RepositoryUniqueConstraintException() : base("A unique constraint violation occurred.") { }
    public RepositoryUniqueConstraintException(string message) : base(message) { }
    public RepositoryUniqueConstraintException(string message, Exception inner) : base(message, inner) { }
}
