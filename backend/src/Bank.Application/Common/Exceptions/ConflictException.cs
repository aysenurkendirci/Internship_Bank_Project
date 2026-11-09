namespace Bank.Application.Common.Exceptions;

/// <summary>The change collides with the current state of a record; answered with 409.</summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}
