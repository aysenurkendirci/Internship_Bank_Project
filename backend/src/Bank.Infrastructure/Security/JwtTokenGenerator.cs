using Bank.Application.Abstractions.Security; 

namespace Bank.Infrastructure.Security;

public class JwtTokenGenerator : ITokenGenerator 
{
    public string Generate(long userId, string? email) => "test-token";
}