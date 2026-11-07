using Bank.Application.Abstractions.Repositories;
using Bank.Application.Common.Exceptions;
using Bank.Domain.Accounts;
using Bank.Domain.ValueObjects;
using Bank.Infrastructure.Persistence;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Bank.Infrastructure.Repositories.Payments;

/// <remarks>
/// Works on the scope's <see cref="DbSession"/> so every call made during a transfer shares one transaction.
/// Locks are taken with UPDLOCK: readers are not blocked, but a second transfer touching the same account (or
/// the same idempotency key) waits until the first one commits or rolls back.
/// </remarks>
public sealed class TransferRepository : ITransferRepository
{
    private const string TransferSelect = """
        SELECT t.TransferId, t.RequestHash, t.SourceAccountId, s.Iban AS SourceIban, d.Iban AS TargetIban,
               CONCAT(u.FirstName, N' ', u.LastName) AS RecipientName, t.Amount, t.CurrencyCode, t.Description,
               l.BalanceAfter AS SourceBalanceAfter, t.CreatedAt
        FROM payments.Transfers t {0}
        JOIN dbo.Accounts s ON s.AccountId = t.SourceAccountId
        JOIN dbo.Accounts d ON d.AccountId = t.TargetAccountId
        JOIN dbo.Users u ON u.UserId = d.UserId
        JOIN payments.LedgerEntries l ON l.TransferId = t.TransferId AND l.Side = 'Debit'
        """;

    private readonly DbSession _session;

    public TransferRepository(DbSession session) => _session = session;

    public async Task<StoredTransfer?> FindByIdempotencyKeyAsync(long userId, string idempotencyKey, CancellationToken ct = default)
    {
        // HOLDLOCK keeps the key range locked even when no row exists yet, so a parallel request with the
        // same key blocks here until this transfer is committed, then finds it.
        var sql = string.Format(TransferSelect, "WITH (UPDLOCK, HOLDLOCK)") + """
             WHERE t.UserId = @UserId AND t.IdempotencyKey = @IdempotencyKey
            """;

        return await QueryTransferAsync(sql, new { UserId = userId, IdempotencyKey = idempotencyKey }, RequireTransaction(), ct);
    }

    public async Task<long?> FindAccountIdAsync(Iban iban, CancellationToken ct = default)
    {
        var connection = await _session.GetConnectionAsync(ct);
        return await connection.ExecuteScalarAsync<long?>(new CommandDefinition(
            "SELECT AccountId FROM dbo.Accounts WHERE Iban = @Iban",
            new { Iban = iban.Value },
            _session.Transaction,
            cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Account>> LockAccountsAsync(IReadOnlyCollection<long> accountIds, CancellationToken ct = default)
    {
        const string sql = """
            SELECT a.AccountId, a.UserId, a.Iban, a.CurrencyCode, a.Balance, a.Status,
                   CONCAT(u.FirstName, N' ', u.LastName) AS HolderName
            FROM dbo.Accounts a WITH (UPDLOCK, ROWLOCK)
            JOIN dbo.Users u ON u.UserId = a.UserId
            WHERE a.AccountId = @AccountId
            """;

        var transaction = RequireTransaction();
        var connection = await _session.GetConnectionAsync(ct);
        var accounts = new List<Account>();

        // One at a time in ascending id order: A->B and B->A both lock the lower id first, so neither can
        // hold one account while waiting for the other.
        foreach (var id in accountIds.Distinct().Order())
        {
            var row = await connection.QuerySingleOrDefaultAsync<LockedAccountRow>(
                new CommandDefinition(sql, new { AccountId = id }, transaction, cancellationToken: ct));

            if (row is not null)
                accounts.Add(new Account(
                    row.AccountId,
                    row.UserId,
                    Iban.Parse(row.Iban),
                    row.HolderName,
                    Enum.Parse<AccountStatus>(row.Status),
                    new Money(row.Balance, Currency.FromCode(row.CurrencyCode))));
        }

        return accounts;
    }

    public async Task<StoredTransfer> AddAsync(NewTransfer transfer, CancellationToken ct = default)
    {
        // Balances move by the amount instead of being overwritten with the computed value: with the rows
        // locked both give the same result, and the relative form cannot lose a change made outside this code.
        // dbo.Transactions keeps feeding the customer-facing statement and the dashboard.
        const string sql = """
            UPDATE dbo.Accounts SET Balance = Balance - @Amount WHERE AccountId = @SourceAccountId;
            UPDATE dbo.Accounts SET Balance = Balance + @Amount WHERE AccountId = @TargetAccountId;

            DECLARE @Created TABLE (TransferId BIGINT);

            INSERT INTO payments.Transfers
                (UserId, IdempotencyKey, RequestHash, SourceAccountId, TargetAccountId, Amount, CurrencyCode, Description)
            OUTPUT INSERTED.TransferId INTO @Created
            VALUES (@UserId, @IdempotencyKey, @RequestHash, @SourceAccountId, @TargetAccountId, @Amount, @CurrencyCode, @Description);

            DECLARE @TransferId BIGINT = (SELECT TransferId FROM @Created);

            INSERT INTO payments.LedgerEntries (TransferId, AccountId, Side, Amount, CurrencyCode, BalanceAfter)
            VALUES (@TransferId, @SourceAccountId, 'Debit', @Amount, @CurrencyCode, @SourceBalanceAfter),
                   (@TransferId, @TargetAccountId, 'Credit', @Amount, @CurrencyCode, @TargetBalanceAfter);

            INSERT INTO dbo.Transactions (AccountId, Amount, Direction, Category, Description)
            VALUES (@SourceAccountId, @Amount, 'Outbound', N'Transfer', @Description),
                   (@TargetAccountId, @Amount, 'Inbound', N'Transfer', @Description);

            SELECT @TransferId;
            """;

        var transaction = RequireTransaction();
        var connection = await _session.GetConnectionAsync(ct);
        var parameters = new
        {
            transfer.UserId,
            transfer.IdempotencyKey,
            transfer.RequestHash,
            SourceAccountId = transfer.Source.Id,
            TargetAccountId = transfer.Target.Id,
            transfer.Amount.Amount,
            CurrencyCode = transfer.Amount.Currency.Code,
            transfer.Description,
            SourceBalanceAfter = transfer.Postings.Debit.BalanceAfter.Amount,
            TargetBalanceAfter = transfer.Postings.Credit.BalanceAfter.Amount
        };

        long transferId;
        try
        {
            transferId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, parameters, transaction, cancellationToken: ct));
        }
        catch (SqlException e) when (e.Number is 2601 or 2627 && e.Message.Contains("UQ_Transfers_IdempotencyKey"))
        {
            // Only reachable if the key lock was bypassed; the unique constraint still refuses a second transfer.
            throw new ConflictException("Aynı Idempotency-Key ile gönderilen bir istek hâlâ işleniyor. Biraz sonra tekrar deneyin.");
        }

        return await GetAsync(transfer.UserId, transferId, ct)
               ?? throw new InvalidOperationException($"Transfer {transferId} was not found right after being written.");
    }

    public async Task<StoredTransfer?> GetAsync(long userId, long transferId, CancellationToken ct = default)
    {
        var sql = string.Format(TransferSelect, string.Empty) + """
             WHERE t.UserId = @UserId AND t.TransferId = @TransferId
            """;

        return await QueryTransferAsync(sql, new { UserId = userId, TransferId = transferId }, _session.Transaction, ct);
    }

    private async Task<StoredTransfer?> QueryTransferAsync(string sql, object parameters, SqlTransaction? transaction, CancellationToken ct)
    {
        var connection = await _session.GetConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<StoredTransfer>(
            new CommandDefinition(sql, parameters, transaction, cancellationToken: ct));
    }

    private SqlTransaction RequireTransaction()
        => _session.Transaction
           ?? throw new InvalidOperationException("Transfers must run inside a unit of work; mark the command ITransactionalCommand.");

    private sealed class LockedAccountRow
    {
        public long AccountId { get; init; }
        public long UserId { get; init; }
        public string Iban { get; init; } = string.Empty;
        public string CurrencyCode { get; init; } = string.Empty;
        public decimal Balance { get; init; }
        public string Status { get; init; } = string.Empty;
        public string HolderName { get; init; } = string.Empty;
    }
}
