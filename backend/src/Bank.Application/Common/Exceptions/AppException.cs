namespace Bank.Application.Common.Exceptions;

/// <summary>
/// Base type for expected failures the client can act on (missing record, duplicate, bad credentials).
/// Anything that does not derive from this type is treated as a server error and its message is never exposed.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}
