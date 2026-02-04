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

        return new CardDetailResponse(
            row.CardId,
            row.CardNo,
            row.CardType,
            row.IsVirtual == "Y",
            row.Status,
            row.AccountId,
            row.AccountType,
            row.Iban,
            row.AccountBalance,
            row.Contactless == "Y",
            row.OnlineUse == "Y",
            row.DailyLimit,
            row.MonthlyLimit,
            row.OwnerFirstName,
            row.Membership
        );
    }
}
