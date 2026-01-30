using Bank.Application.Abstractions.Security;

namespace Bank.Infrastructure.Security; 

public class PasswordHasher : IPasswordHasher 
{
    public string Hash(string password) => password;
    public bool Verify(string password, string passwordHash) => password == passwordHash;
}