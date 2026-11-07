namespace Bank.Application.Common.Exceptions;

/// <summary>
/// Raised for both an unknown identity number and a wrong password. Using one message for both
/// cases keeps the login endpoint from revealing which identity numbers are registered.
/// </summary>
public sealed class AuthenticationFailedException : AppException
{
    public AuthenticationFailedException() : base("TC kimlik numarası veya şifre hatalı.") { }
}
