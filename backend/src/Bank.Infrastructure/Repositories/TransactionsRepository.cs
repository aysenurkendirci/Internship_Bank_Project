using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Transactions;
using Bank.Infrastructure.Oracle;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Bank.Infrastructure.Repositories;

public sealed class TransactionsRepository : ITransactionsRepository
{
    private readonly OracleExecutor _db;
    public TransactionsRepository(OracleExecutor db) => _db = db;

    private sealed class RecentTxRow
    {
        public long TxId { get; init; }
        public long AccountId { get; init; }
        public long? CardId { get; init; }
        public decimal Amount { get; init; }
        public string Direction { get; init; } = "";
        public string Category { get; init; } = "";
        public string Description { get; init; } = "";
        public DateTime CreatedAt { get; init; }
    }

public async Task<IReadOnlyList<TransactionItem>> GetRecentAsync(
    long userId,
    long? accountId,
    long? cardId,
    int take,
    CancellationToken ct)
{
    if (accountId is not null && cardId is not null)
        throw new ArgumentException("Send either accountId or cardId, not both.");

    var p = new OracleDynamicParameters();
    p.Add("P_USER_ID", userId, OracleDbType.Int64, ParameterDirection.Input);
    p.Add("P_ACCOUNT_ID", accountId, OracleDbType.Int64, ParameterDirection.Input);
    p.Add("P_CARD_ID", cardId, OracleDbType.Int64, ParameterDirection.Input);
    p.Add("P_TAKE", take, OracleDbType.Int32, ParameterDirection.Input);
    p.Add("O_CURSOR", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);

    var rows = await _db.QueryAsync<RecentTxRow>(
        "GENCBANK.PKG_DASHBOARD.GET_RECENT_TRANSACTIONS", p);

    return rows.Select(r => new TransactionItem(
        r.TxId,
        r.Description,
        r.Category,
        r.Amount,
        r.Direction,
        r.CreatedAt
    )).ToList();
}

}
