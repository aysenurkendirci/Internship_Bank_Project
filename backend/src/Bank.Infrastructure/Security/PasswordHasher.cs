using Bank.Application.Abstractions.Security;

namespace Bank.Infrastructure.Security; // BU SATIR ÇOK KRİTİK

public class PasswordHasher : IPasswordHasher 
{
    public string Hash(string password) => password;
    public bool Verify(string password, string passwordHash) => password == passwordHash;
}