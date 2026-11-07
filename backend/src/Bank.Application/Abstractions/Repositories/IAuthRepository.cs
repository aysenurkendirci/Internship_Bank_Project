using Bank.Contracts.Auth;

namespace Bank.Application.Abstractions.Repositories;

public interface IAuthRepository
{
    Task<UserRow?> GetUserByTcOrDefaultAsync(string tcNo, CancellationToken ct = default);
    Task UpdateLastLoginAsync(long userId, CancellationToken ct = default);
    Task<UserRow> CreateUserAsync(RegisterRequest req, string passwordHash, CancellationToken ct = default);
    Task<CredentialRow> GetCredentialsAsync(long userId, CancellationToken ct = default);
    Task<UserProfileRow> GetProfileAsync(long userId, CancellationToken ct = default);
}

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
