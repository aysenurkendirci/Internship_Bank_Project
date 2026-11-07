using Bank.Application.Abstractions.Repositories;
using Bank.Infrastructure.Persistence;
using Dapper;

namespace Bank.Infrastructure.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly SqlConnectionFactory _connections;

    public AccountRepository(SqlConnectionFactory connections) => _connections = connections;

    public async Task<decimal> GetAvailableBalanceAsync(long userId, string currencyCode, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COALESCE(SUM(Balance), 0)
            FROM dbo.Accounts
            WHERE UserId = @UserId AND Status = 'Active' AND CurrencyCode = @CurrencyCode
            """;

        await using var connection = await _connections.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(sql, new { UserId = userId, CurrencyCode = currencyCode }, cancellationToken: ct));
    }
}
