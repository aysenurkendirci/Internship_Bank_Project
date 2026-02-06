using Bank.Contracts.Auth;
using System.Threading.Tasks;

namespace Bank.Application.Abstractions.Repositories; 

public interface IAuthRepository
{
    Task<UserRow> GetUserByTcAsync(string tcNo);
    Task<UserRow?> GetUserByTcOrDefaultAsync(string tcNo);
    Task UpdateLastLoginAsync(long userId);
    Task<UserRow> CreateUserAsync(RegisterRequest req, string passwordHash);
    Task<CredentialRow> GetCredentialsAsync(long userId);
}

public sealed class UserRow 
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