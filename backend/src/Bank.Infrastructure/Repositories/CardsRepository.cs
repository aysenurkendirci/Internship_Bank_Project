using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Cards;
using Bank.Infrastructure.Oracle;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using Bank.Infrastructure.Repositories; // or the correct namespace where CardDetailRow is defined

namespace Bank.Infrastructure.Repositories;

public sealed class CardsRepository : ICardsRepository
{
    private readonly OracleExecutor _db;

    public CardsRepository(OracleExecutor db) => _db = db;

    public async Task<CardDetailResponse?> GetCardDetailAsync(long cardId, CancellationToken ct = default)
    {
        var p = new OracleDynamicParameters();
        p.Add("p_card_id", cardId, OracleDbType.Int64, ParameterDirection.Input);
        p.Add("o_cursor", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);

        var row = await _db.QuerySingleAsync<CardDetailRow>(
            "PKG_DASHBOARD.GET_CARD_DETAIL",
            p
        );

        if (row is null) return null;
return new CardDetailResponse
{
    CardId = (long)row.CardId,
    CardNo = row.CardNo,
    CardType = row.CardType,
    IsVirtual = row.IsVirtual == "Y",
    Status = row.Status,

    AccountId = (long)row.AccountId,
    AccountType = row.AccountType,
    Iban = row.Iban,
    AccountBalance = row.AccountBalance,

    Contactless = row.Contactless == "Y",
    OnlineUse = row.OnlineUse == "Y",
    DailyLimit = row.DailyLimit,
    MonthlyLimit = row.MonthlyLimit,

    OwnerFirstName = row.OwnerFirstName,
    Membership = row.Membership
};

    }
}
