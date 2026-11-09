namespace Bank.Application.Abstractions.Repositories;

/// <summary>Storage of customers and their credentials.</summary>
public interface IAuthRepository
{
    Task<UserRow?> GetUserByTcOrDefaultAsync(string tcNo, CancellationToken ct = default);
    Task UpdateLastLoginAsync(long userId, CancellationToken ct = default);
    Task<UserRow> CreateUserAsync(NewUser user, CancellationToken ct = default);
    Task<CredentialRow> GetCredentialsAsync(long userId, CancellationToken ct = default);
    Task<UserProfileRow> GetProfileAsync(long userId, CancellationToken ct = default);
    Task UpdateContactAsync(long userId, string email, string phone, CancellationToken ct = default);
    Task UpdatePasswordAsync(long userId, string passwordHash, CancellationToken ct = default);
}

/// <summary>What is stored for a new customer; the password is already hashed by the time it gets here.</summary>
public sealed record NewUser(
    string TcNo, string FirstName, string LastName, string Email, string Phone, string Membership, string PasswordHash);

public sealed class UserRow
{
    public long UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string TcNo { get; init; } = string.Empty;
}

public sealed class UserProfileRow
{
    public string TcNo { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Membership { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public sealed class CredentialRow
{
    public long UserId { get; init; }
    public string PasswordHash { get; init; } = string.Empty;
}
