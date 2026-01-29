using Bank.Application.Abstractions.Security; // Arayüzü tanıması için şart

namespace Bank.Infrastructure.Security;

// ": ITokenGenerator" kısmının olduğundan ve doğru yazıldığından emin ol
public class JwtTokenGenerator : ITokenGenerator 
{
    public string Generate(long userId, string? email) => "test-token";
}