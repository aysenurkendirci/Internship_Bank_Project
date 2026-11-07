using Bank.Contracts.Auth;
using System.Threading.Tasks;

namespace Bank.Application.Abstractions.Repositories; // Namespace bu olmalı

public interface IAuthRepository
{
    Task<UserRow> GetUserByTcAsync(string tcNo, CancellationToken ct = default);
    Task<UserRow?> GetUserByTcOrDefaultAsync(string tcNo, CancellationToken ct = default);
    Task UpdateLastLoginAsync(long userId, CancellationToken ct = default);
    Task<UserRow> CreateUserAsync(RegisterRequest req, string passwordHash, CancellationToken ct = default);
    Task<CredentialRow> GetCredentialsAsync(long userId, CancellationToken ct = default);
}

public sealed class UserRow // Burayı record'dan class'a çevirmiştik
{
    public UserRow() { }
    public long UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string TcNo { get; set; } = string.Empty;
}

public sealed class CredentialRow
{
    public CredentialRow() { }
    public long UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
}