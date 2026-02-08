using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Transfers;
using Bank.Infrastructure.Oracle;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Bank.Infrastructure.Repositories;

public sealed class TransfersRepository : ITransfersRepository
{
    private readonly OracleExecutor _db;

    public TransfersRepository(OracleExecutor db) => _db = db;

    public async Task<TransferResponse> CreateAsync(CreateTransferRequest req, CancellationToken ct = default)
    {
        var p = new OracleDynamicParameters();

        var toAccountId = req.ToAccountId is > 0 ? req.ToAccountId : null;
        var toCardId    = req.ToCardId    is > 0 ? req.ToCardId    : null;

        p.Add("p_from_account_id", req.FromAccountId, OracleDbType.Int64, ParameterDirection.Input);
        p.Add("p_to_account_id",   toAccountId,       OracleDbType.Int64, ParameterDirection.Input);
        p.Add("p_to_card_id",      toCardId,          OracleDbType.Int64, ParameterDirection.Input);
        p.Add("p_amount",          req.Amount,        OracleDbType.Decimal, ParameterDirection.Input);

        p.Add("p_note", req.Note, OracleDbType.Varchar2, ParameterDirection.Input, size: 200);

        p.Add("o_status", null, OracleDbType.Varchar2, ParameterDirection.Output, size: 50);

        await _db.ExecuteAsync("GENCBANK.PKG_TRANSFERS.CREATE_TRANSFER", p);

        var status = p.GetValue("o_status")?.ToString() ?? "UNKNOWN";
        return new TransferResponse { Status = status };
    }
}
