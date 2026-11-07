using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Dashboard;
using Bank.Infrastructure.Persistence;
using Dapper;

namespace Bank.Infrastructure.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private const int RecentTransactionCount = 5;

    private readonly SqlConnectionFactory _connections;

    public DashboardRepository(SqlConnectionFactory connections) => _connections = connections;

    public async Task<DashboardResponse> GetDashboardAsync(long userId, CancellationToken ct = default)
    {
        // All five result sets are fetched in a single round trip.
        // TotalWealth only sums TRY accounts: adding balances in different currencies
        // without an exchange rate would produce a meaningless number.
        const string sql = """
            SELECT UserId, FirstName, Membership
            FROM dbo.Users
            WHERE UserId = @UserId;

            SELECT COALESCE(SUM(Balance), 0)
            FROM dbo.Accounts
            WHERE UserId = @UserId AND Status = 'Active' AND CurrencyCode = 'TRY';

            SELECT c.CardId, c.CardLastFour, c.CardType, c.IsVirtual, c.Status,
                   a.Balance AS AccountBalance, c.Contactless, c.OnlineUse, c.DailyLimit, c.MonthlyLimit
            FROM dbo.Cards c
            JOIN dbo.Accounts a ON a.AccountId = c.AccountId
            WHERE c.UserId = @UserId
            ORDER BY c.CardId;

            SELECT TOP (@Take) t.TransactionId, t.Description, t.Category, t.Amount, t.Direction, t.CreatedAt
            FROM dbo.Transactions t
            JOIN dbo.Accounts a ON a.AccountId = t.AccountId
            WHERE a.UserId = @UserId
            ORDER BY t.CreatedAt DESC;

            SELECT AccountId, AccountType, Iban, Balance, Status
            FROM dbo.Accounts
            WHERE UserId = @UserId
            ORDER BY AccountId;
            """;

        await using var connection = await _connections.OpenAsync(ct);
        await using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId, Take = RecentTransactionCount }, cancellationToken: ct));

        var user = await results.ReadSingleOrDefaultAsync<UserSummaryRow>()
                   ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");
        var totalWealth = await results.ReadSingleAsync<decimal>();
        var cards = await results.ReadAsync<CardRow>();
        var transactions = await results.ReadAsync<TransactionRow>();
        var accounts = await results.ReadAsync<AccountRow>();

        return new DashboardResponse(
            new UserSummary(user.UserId, user.FirstName, user.Membership),
            totalWealth,
            0.024m,
            cards.Select(c => new CardItem(
                c.CardId,
                $"**** **** **** {c.CardLastFour}",
                c.CardType,
                c.IsVirtual,
                c.Status,
                c.AccountBalance,
                new CardSettings(c.Contactless, c.OnlineUse),
                new CardLimits(c.DailyLimit, c.MonthlyLimit)
            )).ToList(),
            transactions.Select(t => new TransactionItem(
                t.TransactionId,
                t.Description,
                t.Category,
                t.Amount,
                t.Direction,
                t.CreatedAt
            )).ToList(),
            accounts.Select(a => new AccountItem(
                a.AccountId,
                a.AccountType switch
                {
                    "Demand" => "Vadesiz TL",
                    "Time" => "Vadeli Mevduat",
                    _ => "Dolar Hesabı"
                },
                a.Iban,
                a.Balance,
                a.Status,
                a.AccountType == "Time" ? "%32 Faiz" : "Aktif",
                a.AccountType switch
                {
                    "Demand" => "bank",
                    "Time" => "trend",
                    _ => "wallet"
                }
            )).ToList()
        );
    }

    // Row types mirror the SELECT column lists above; they never leave this class.

    private sealed class UserSummaryRow
    {
        public long UserId { get; init; }
        public string FirstName { get; init; } = "";
        public string Membership { get; init; } = "";
    }

    private sealed class CardRow
    {
        public long CardId { get; init; }
        public string CardLastFour { get; init; } = "";
        public string CardType { get; init; } = "";
        public bool IsVirtual { get; init; }
        public string Status { get; init; } = "";
        public decimal AccountBalance { get; init; }
        public bool Contactless { get; init; }
        public bool OnlineUse { get; init; }
        public decimal DailyLimit { get; init; }
        public decimal MonthlyLimit { get; init; }
    }

    private sealed class TransactionRow
    {
        public long TransactionId { get; init; }
        public string Description { get; init; } = "";
        public string Category { get; init; } = "";
        public decimal Amount { get; init; }
        public string Direction { get; init; } = "";
        public DateTime CreatedAt { get; init; }
    }

    private sealed class AccountRow
    {
        public long AccountId { get; init; }
        public string AccountType { get; init; } = "";
        public string Iban { get; init; } = "";
        public decimal Balance { get; init; }
        public string Status { get; init; } = "";
    }
}
