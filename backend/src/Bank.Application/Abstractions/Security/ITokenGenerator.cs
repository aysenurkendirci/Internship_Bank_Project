namespace Bank.Application.Abstractions.Security;

public interface ITokenGenerator
{
    string Generate(long userId, string? email);
}
