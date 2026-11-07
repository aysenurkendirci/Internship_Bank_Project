using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Auth;
using Bank.Infrastructure.Persistence;
using Dapper;

namespace Bank.Infrastructure.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly SqlConnectionFactory _connections;

    public AuthRepository(SqlConnectionFactory connections) => _connections = connections;

    public async Task<UserRow> GetUserByTcAsync(string tcNo, CancellationToken ct = default)
        => await GetUserByTcOrDefaultAsync(tcNo, ct)
           ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");

    public async Task<UserRow?> GetUserByTcOrDefaultAsync(string tcNo, CancellationToken ct = default)
    {
        const string sql = """
            SELECT UserId, Email, FirstName, LastName, TcNo
            FROM dbo.Users
            WHERE TcNo = @TcNo
            """;

        await using var connection = await _connections.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(sql, new { TcNo = tcNo }, cancellationToken: ct));
    }

    public async Task<CredentialRow> GetCredentialsAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT UserId, PasswordHash
            FROM dbo.Users
            WHERE UserId = @UserId
            """;

        await using var connection = await _connections.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<CredentialRow>(
                   new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct))
               ?? throw new KeyNotFoundException("Kimlik bilgileri bulunamadı.");
    }

    public async Task<UserRow> CreateUserAsync(RegisterRequest req, string passwordHash, CancellationToken ct = default)
    {
        // OUTPUT returns the inserted row in the same round trip, so no follow-up SELECT is needed.
        const string sql = """
            INSERT INTO dbo.Users (TcNo, FirstName, LastName, Email, Phone, Membership, PasswordHash)
            OUTPUT INSERTED.UserId, INSERTED.Email, INSERTED.FirstName, INSERTED.LastName, INSERTED.TcNo
            VALUES (@TcNo, @FirstName, @LastName, @Email, @Phone, @Membership, @PasswordHash)
            """;

        var parameters = new
        {
            req.TcNo,
            req.FirstName,
            req.LastName,
            req.Email,
            req.Phone,
            req.Membership,
            PasswordHash = passwordHash
        };

        await using var connection = await _connections.OpenAsync(ct);
        return await connection.QuerySingleAsync<UserRow>(
            new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    public async Task UpdateLastLoginAsync(long userId, CancellationToken ct = default)
    {
        const string sql = "UPDATE dbo.Users SET LastLoginAt = SYSUTCDATETIME() WHERE UserId = @UserId";

        await using var connection = await _connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
    }
}
