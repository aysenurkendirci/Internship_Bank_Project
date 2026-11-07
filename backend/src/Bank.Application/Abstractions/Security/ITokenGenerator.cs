namespace Bank.Application.Abstractions.Security;

public interface ITokenGenerator
{
    /// <summary>
    /// The token carries the user id only. A JWT is signed, not encrypted: anything put in it can be read by
    /// whoever sees the token, including proxies that log headers.
    /// </summary>
    string Generate(long userId);
}
