using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Dashboard;
using Bank.Infrastructure.Oracle;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Bank.Infrastructure.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly OracleExecutor _db;

    public DashboardRepository(OracleExecutor db)
    {
        _db = db;
    }

    public async Task<DashboardResponse> GetDashboardAsync(long userId, CancellationToken ct = default)
    {
        // 1) User summary (PKG_DASHBOARD.GET_USER_SUMMARY)
        var summaryParams = new OracleDynamicParameters();
        summaryParams.Add("p_user_id", userId);
        summaryParams.Add("o_cursor", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);

        var user = await _db.QuerySingleAsync<UserSummaryRow>(
            "PKG_DASHBOARD.GET_USER_SUMMARY",
            summaryParams
        );

        if (user is null)
            throw new KeyNotFoundException($"Kullanıcı bulunamadı. userId={userId}");

        // 2) Total wealth (PKG_DASHBOARD.GET_TOTAL_WEALTH)
        var wealthParams = new OracleDynamicParameters();
        wealthParams.Add("p_user_id", userId);
        wealthParams.Add("o_cursor", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);

        var wealth = await _db.QuerySingleAsync<TotalWealthRow>(
            "PKG_DASHBOARD.GET_TOTAL_WEALTH",
            wealthParams
        );
        var totalWealth = wealth?.TOTAL_WEALTH ?? 0m;

        // 3) Cards list (PKG_DASHBOARD.GET_CARDS)
        var cardsParams = new OracleDynamicParameters();
        cardsParams.Add("p_user_id", userId);
        cardsParams.Add("o_cursor", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);

        var cardsRows = await _db.QueryAsync<CardRow>(
            "PKG_DASHBOARD.GET_CARDS",
            cardsParams
        );

        // 4) Recent transactions list (PKG_DASHBOARD.GET_RECENT_TRANSACTIONS)
        var txParams = new OracleDynamicParameters();
        txParams.Add("p_user_id", userId);
        txParams.Add("p_take", 5);
        txParams.Add("o_cursor", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);

        var txRows = await _db.QueryAsync<TransactionRow>(
            "PKG_DASHBOARD.GET_RECENT_TRANSACTIONS",
            txParams
        );

        // 5) Map -> Contracts DTO
        var cards = new List<CardItem>(cardsRows.Count);
        foreach (var c in cardsRows)
        {
            cards.Add(new CardItem(
                c.CARD_ID,
                MaskCard(c.CARD_NO),
                c.CARD_TYPE,
                c.IS_VIRTUAL == "Y",
                c.STATUS,
                c.ACCOUNT_BALANCE,
                new CardSettings(
                    c.CONTACTLESS == "Y",
                    c.ONLINE_USE == "Y"
                ),
                new CardLimits(
                    c.DAILY_LIMIT,
                    c.MONTHLY_LIMIT
                )
            ));
        }

        var recent = new List<TransactionItem>(txRows.Count);
        foreach (var t in txRows)
        {
            recent.Add(new TransactionItem(
                t.TX_ID,
                t.DESCRIPTION,
                t.CATEGORY,
                t.AMOUNT,
                t.DIRECTION,
                t.CREATED_AT
            ));
        }

        var wealthChangeRate = 0.024m; // MVP Sabit Değer

        return new DashboardResponse(
            new UserSummary(user.USER_ID, user.FIRST_NAME, user.MEMBERSHIP),
            totalWealth,
            wealthChangeRate,
            cards,
            recent
        );
    }

    private static string MaskCard(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length < 4) return "****";
        var last4 = raw[^4..];
        return $"**** **** **** {last4}";
    }

    // --- Oracle row types (DB kolon isimleri ile birebir) ---
    private sealed class UserSummaryRow
    {
        public long USER_ID { get; set; }
        public string FIRST_NAME { get; set; } = "";
        public string MEMBERSHIP { get; set; } = "";
    }

    private sealed class TotalWealthRow
    {
        public decimal TOTAL_WEALTH { get; set; }
    }

    private sealed class CardRow
    {
        public long CARD_ID { get; set; }
        public string CARD_NO { get; set; } = "";
        public string CARD_TYPE { get; set; } = "";
        public string IS_VIRTUAL { get; set; } = "N";
        public string STATUS { get; set; } = "";
        public decimal ACCOUNT_BALANCE { get; set; }
        public string CONTACTLESS { get; set; } = "N";
        public string ONLINE_USE { get; set; } = "N";
        public decimal DAILY_LIMIT { get; set; }
        public decimal MONTHLY_LIMIT { get; set; }
    }

    private sealed class TransactionRow
    {
        public long TX_ID { get; set; }
        public decimal AMOUNT { get; set; }
        public string DIRECTION { get; set; } = "";
        public string CATEGORY { get; set; } = "";
        public string DESCRIPTION { get; set; } = "";
        public DateTime CREATED_AT { get; set; }
    }
}