using Bank.Infrastructure.Oracle;
using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Accounts;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Bank.Infrastructure.Repositories;

public sealed class AccountsRepository : IAccountsRepository
{
    private readonly OracleExecutor _db;

    public AccountsRepository(OracleExecutor db) => _db = db;

    public async Task<AccountDetailResponse?> GetAccountDetailAsync(long accountId, CancellationToken ct = default)
    {
        var p = new OracleDynamicParameters();
        p.Add("p_account_id", accountId, OracleDbType.Int64, ParameterDirection.Input);
        p.Add("o_cursor", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);

        var row = await _db.QuerySingleAsync<AccountDetailRow>(
            "PKG_DASHBOARD.GET_ACCOUNT_DETAIL",
            p
        );

        if (row is null) return null;

        return new AccountDetailResponse(
            row.AccountId,
            row.Type,
            row.Iban,
            row.Balance,
            row.Status,
            row.CreatedAt,
            row.OwnerFirstName,
            row.Membership
        );
    }
}
