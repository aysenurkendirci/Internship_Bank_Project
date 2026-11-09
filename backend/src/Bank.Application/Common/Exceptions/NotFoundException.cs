namespace Bank.Application.Common.Exceptions;

/// <summary>The record is missing or belongs to someone else; both are answered the same way, with 404.</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}
