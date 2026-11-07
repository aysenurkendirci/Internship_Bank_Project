namespace Bank.Domain.Common;

/// <summary>
/// Raised when an operation would break a business rule, e.g. adding amounts in different currencies.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
