namespace Bank.Application.Common.Exceptions;

/// <summary>The request is well-formed but cannot be carried out; answered with 400.</summary>
public sealed class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message) { }
}
