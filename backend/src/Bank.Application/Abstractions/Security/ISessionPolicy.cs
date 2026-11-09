namespace Bank.Application.Abstractions.Security;

public interface ISessionPolicy
{
    /// <summary>
    /// How long a refresh token stays valid after it was issued. Every refresh issues a new token, so an
    /// active session keeps sliding forward while an abandoned one expires.
    /// </summary>
    TimeSpan RefreshTokenLifetime { get; }
}
