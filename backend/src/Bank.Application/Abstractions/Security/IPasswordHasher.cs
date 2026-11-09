namespace Bank.Application.Abstractions.Security;

/// <summary>Hashes and verifies passwords; the algorithm stays an infrastructure detail.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}