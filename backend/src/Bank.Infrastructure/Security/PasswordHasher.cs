using Bank.Application.Abstractions.Security;
using Microsoft.AspNetCore.Identity;

namespace Bank.Infrastructure.Security;

/// <summary>
/// Delegates to ASP.NET Core Identity's hasher: PBKDF2 with a random per-password salt,
/// constant-time comparison and a versioned output format that allows raising the
/// work factor later without invalidating existing hashes.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    // The Identity hasher is generic over a user type it does not actually use.
    private static readonly PasswordHasher<object> Inner = new();

    public string Hash(string password)
        => Inner.HashPassword(user: null!, password);

    public bool Verify(string password, string passwordHash)
        => Inner.VerifyHashedPassword(user: null!, passwordHash, password)
           != PasswordVerificationResult.Failed;
}
