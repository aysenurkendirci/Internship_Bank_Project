using Bank.Application.Abstractions.Repositories;
using Bank.Domain.Accounts;
using Bank.Infrastructure.Persistence;
using Dapper;

namespace Bank.Infrastructure.Repositories;

/// <summary>Reads accounts and opens new ones. Moving money is TransferRepository's job.</summary>
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

    public async Task<IReadOnlyList<AccountRow>> ListAsync(long userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT AccountId, Iban, AccountType, CurrencyCode, Balance, Status
            FROM dbo.Accounts
            WHERE UserId = @UserId
            ORDER BY AccountId
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var rows = await connection.QueryAsync<AccountRow>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<long> NextAccountNumberAsync(CancellationToken ct = default)
    {
        await using var connection = await _connections.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition("SELECT NEXT VALUE FOR payments.AccountNumbers", cancellationToken: ct));
    }

    public async Task<AccountRow> OpenAsync(
        long userId, string accountType, string currencyCode, Iban iban, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Accounts (UserId, AccountType, CurrencyCode, Iban, Balance)
            OUTPUT INSERTED.AccountId, INSERTED.Iban, INSERTED.AccountType, INSERTED.CurrencyCode, INSERTED.Balance, INSERTED.Status
            VALUES (@UserId, @AccountType, @CurrencyCode, @Iban, 0)
            """;

        await using var connection = await _connections.OpenAsync(ct);
        return await connection.QuerySingleAsync<AccountRow>(new CommandDefinition(
            sql,
            new { UserId = userId, AccountType = accountType, CurrencyCode = currencyCode, Iban = iban.Value },
            cancellationToken: ct));
    }

    public async Task<bool> BelongsToAsync(long userId, long accountId, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.Accounts WHERE AccountId = @AccountId AND UserId = @UserId";

        await using var connection = await _connections.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { AccountId = accountId, UserId = userId }, cancellationToken: ct)) > 0;
    }

    public async Task<(IReadOnlyList<StatementEntryRow> Items, int TotalCount)> ListStatementAsync(
        long accountId, int page, int pageSize, CancellationToken ct = default)
    {
        // The counterparty is the other account of the transfer: the target for a debit, the source for a credit.
        const string sql = """
            SELECT COUNT(*)
            FROM payments.LedgerEntries
            WHERE AccountId = @AccountId;

            SELECT l.EntryId, l.TransferId, l.Side, l.Amount, l.CurrencyCode, l.BalanceAfter, t.Description,
                   other.Iban AS CounterpartyIban, l.CreatedAt
            FROM payments.LedgerEntries l
            JOIN payments.Transfers t ON t.TransferId = l.TransferId
            JOIN dbo.Accounts other
              ON other.AccountId = CASE l.Side WHEN 'Debit' THEN t.TargetAccountId ELSE t.SourceAccountId END
            WHERE l.AccountId = @AccountId
            ORDER BY l.EntryId DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        var parameters = new { AccountId = accountId, Skip = (page - 1) * pageSize, Take = pageSize };

        await using var connection = await _connections.OpenAsync(ct);
        await using var results = await connection.QueryMultipleAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));

        var total = await results.ReadSingleAsync<int>();
        var items = (await results.ReadAsync<StatementEntryRow>()).AsList();
        return (items, total);
    }
}
