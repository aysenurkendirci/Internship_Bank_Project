using Bank.Application.Abstractions.Repositories;
using Bank.Application.Common.Exceptions;
using Bank.Infrastructure.Persistence;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Bank.Infrastructure.Repositories;

/// <summary>Customers and credentials in dbo.Users.</summary>
public sealed class AuthRepository : IAuthRepository
{
    // SQL Server error numbers for unique constraint / unique index violations.
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    private readonly SqlConnectionFactory _connections;

    public AuthRepository(SqlConnectionFactory connections) => _connections = connections;

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
               ?? throw new InvalidOperationException($"No credentials stored for user {userId}.");
    }

    public async Task<UserRow> CreateUserAsync(NewUser user, CancellationToken ct = default)
    {
        // OUTPUT returns the inserted row in the same round trip, so no follow-up SELECT is needed.
        const string sql = """
            INSERT INTO dbo.Users (TcNo, FirstName, LastName, Email, Phone, Membership, PasswordHash)
            OUTPUT INSERTED.UserId, INSERTED.Email, INSERTED.FirstName, INSERTED.LastName, INSERTED.TcNo
            VALUES (@TcNo, @FirstName, @LastName, @Email, @Phone, @Membership, @PasswordHash)
            """;

        await using var connection = await _connections.OpenAsync(ct);
        try
        {
            return await connection.QuerySingleAsync<UserRow>(
                new CommandDefinition(sql, user, cancellationToken: ct));
        }
        catch (SqlException ex) when (ex.Number is UniqueConstraintViolation or UniqueIndexViolation)
        {
            // The handler checks for duplicates first, but two concurrent requests can both pass
            // that check. The unique constraint is the real guarantee; report it as a conflict.
            throw new ConflictException("Bu TC kimlik numarası veya e-posta ile kayıtlı bir kullanıcı var.");
        }
    }

    public async Task<UserProfileRow> GetProfileAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TcNo, FirstName, LastName, Email, Phone, Membership, CreatedAt, LastLoginAt
            FROM dbo.Users
            WHERE UserId = @UserId
            """;

        await using var connection = await _connections.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<UserProfileRow>(
                   new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct))
               ?? throw new NotFoundException("Kullanıcı bulunamadı.");
    }

    public async Task UpdateLastLoginAsync(long userId, CancellationToken ct = default)
    {
        const string sql = "UPDATE dbo.Users SET LastLoginAt = SYSUTCDATETIME() WHERE UserId = @UserId";

        await using var connection = await _connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
    }

    public async Task UpdateContactAsync(long userId, string email, string phone, CancellationToken ct = default)
    {
        const string sql = "UPDATE dbo.Users SET Email = @Email, Phone = @Phone WHERE UserId = @UserId";

        await using var connection = await _connections.OpenAsync(ct);
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId, Email = email, Phone = phone }, cancellationToken: ct));
        }
        catch (SqlException ex) when (ex.Number is UniqueConstraintViolation or UniqueIndexViolation)
        {
            throw new ConflictException("Bu e-posta adresi başka bir hesapta kullanılıyor.");
        }
    }

    public async Task UpdatePasswordAsync(long userId, string passwordHash, CancellationToken ct = default)
    {
        const string sql = "UPDATE dbo.Users SET PasswordHash = @PasswordHash WHERE UserId = @UserId";

        await using var connection = await _connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId, PasswordHash = passwordHash }, cancellationToken: ct));
    }
}
