using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Dashboard;
using Bank.Infrastructure.Oracle;
using Oracle.ManagedDataAccess.Client;
using System.Data;

using ContractsAccountItem = Bank.Contracts.Dashboard.AccountItem;

namespace Bank.Infrastructure.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly OracleExecutor _db;

    public DashboardRepository(OracleExecutor db) => _db = db;

    public async Task<DashboardResponse> GetDashboardAsync(long userId, CancellationToken ct = default)
    {
        var user = await _db.QuerySingleAsync<UserSummaryRow>(
            "PKG_DASHBOARD.GET_USER_SUMMARY",
            CreateParams(userId)
        );

        if (user is null)
            throw new KeyNotFoundException($"Kullanıcı bulunamadı. ID: {userId}");

        var wealth = await _db.QuerySingleAsync<TotalWealthRow>(
            "PKG_DASHBOARD.GET_TOTAL_WEALTH",
            CreateParams(userId)
        );

        var cardRows = await _db.QueryAsync<CardRow>(
            "PKG_DASHBOARD.GET_CARDS",
            CreateParams(userId)
        );

        var txParams = CreateParams(userId);
        txParams.Add("p_take", 5);

        var txRows = await _db.QueryAsync<TransactionRow>(
            "PKG_DASHBOARD.GET_RECENT_TRANSACTIONS",
            txParams
        );

        var accRows = await _db.QueryAsync<AccountRow>(
            "PKG_DASHBOARD.GET_ACCOUNTS",
            CreateParams(userId)
        );

        var accounts = accRows.Select(a => {
    var type = (a.TYPE ?? "").Trim().ToUpperInvariant();

    string accountName = type switch
    {
        "VADESIZ" => "Vadesiz TL",
        "VADELI" => "Vadeli Mevduat",
        "TRY" or "TL" => "TL Hesabı",
        "USD" or "DOLLAR" or "DOLAR" => "Dolar Hesabı",
        "EUR" or "EURO" => "Euro Hesabı",
        "YATIRIM" or "INVESTMENT" => "Yatırım Hesabı",
        _ => string.IsNullOrWhiteSpace(a.TYPE) ? "Banka Hesabı" : a.TYPE // Fallback: DB'deki orijinal değeri bas
    };

    string subtitle = type == "VADELI" ? "%32 Faiz" : "Aktif";

    string icon = type switch
    {
        "VADESIZ" or "TRY" or "TL" => "bank",
        "VADELI" => "trend",
        "USD" or "EUR" or "EURO" => "wallet",
        _ => "payments"
    };

    return new ContractsAccountItem(
        a.ACCOUNT_ID,
        accountName,
        a.IBAN,
        a.BALANCE,
        a.STATUS,
        subtitle,
        icon
    );
}).ToList();

        return new DashboardResponse(
            new UserSummary(user.USER_ID, user.FIRST_NAME, user.MEMBERSHIP),
            wealth?.TOTAL_WEALTH ?? 0m,
            0.024m,
            cardRows.Select(c => new CardItem(
                c.CARD_ID,
                Mask(c.CARD_NO),
                c.CARD_TYPE,
                c.IS_VIRTUAL == "Y",
                c.STATUS,
                c.ACCOUNT_BALANCE,
                new CardSettings(c.CONTACTLESS == "Y", c.ONLINE_USE == "Y"),
                new CardLimits(c.DAILY_LIMIT, c.MONTHLY_LIMIT)
            )).ToList(),
            txRows.Select(t => new TransactionItem(
                t.TX_ID,
                t.DESCRIPTION,
                t.CATEGORY,
                t.AMOUNT,
                t.DIRECTION,
                t.CREATED_AT
            )).ToList(),
            accounts
        );
    }

    public async Task<IReadOnlyList<SavingsGoalRow>> GetSavingsGoalsAsync(long userId, CancellationToken ct = default)
    {
        var rows = await _db.QueryAsync<SavingsGoalRow>(
            "PKG_DASHBOARD.GET_SAVINGS_GOALS",
            CreateParams(userId)
        );

        return rows.ToList();
    }

    public async Task CreateSavingsGoalAsync(long userId, CreateSavingsGoalRequest req, CancellationToken ct = default)
    {
        var p = new OracleDynamicParameters();
        p.Add("p_user_id", userId);
        p.Add("p_title", req.Title);
        p.Add("p_target_amount", req.TargetAmount);

        await _db.ExecuteAsync(
            "PKG_DASHBOARD.CREATE_SAVINGS_GOAL",
            p
        );
    }


    private OracleDynamicParameters CreateParams(long userId)
    {
        var p = new OracleDynamicParameters();
        p.Add("p_user_id", userId);
        p.Add("o_cursor", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);
        return p;
    }

    private string Mask(string s)
        => string.IsNullOrWhiteSpace(s) || s.Length < 4
            ? "****"
            : $"**** **** **** {s[^4..]}";


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

    private sealed class AccountRow
    {
        public long ACCOUNT_ID { get; set; }
        public string TYPE { get; set; } = "";
        public string IBAN { get; set; } = "";
        public decimal BALANCE { get; set; }
        public string STATUS { get; set; } = "";
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
