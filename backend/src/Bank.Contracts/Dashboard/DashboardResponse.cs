namespace Bank.Contracts.Dashboard;

public sealed record DashboardResponse(
    UserSummary User,
    decimal TotalWealth,
    IReadOnlyList<CardItem> Cards,
    IReadOnlyList<TransactionItem> RecentTransactions,
    IReadOnlyList<AccountItem> Accounts
);

public sealed record UserSummary(long UserId, string FirstName, string Membership);

public sealed record CardItem(
    long CardId,
    string CardNoMasked,
    string CardType,
    bool IsVirtual,
    string Status,
    decimal Balance,
    CardSettings? Settings,
    CardLimits? Limits
);

public sealed record CardSettings(bool Contactless, bool OnlineUse);
public sealed record CardLimits(decimal DailyLimit, decimal MonthlyLimit);

public sealed record TransactionItem(
    long TxId,
    string Title,
    string Category,
    decimal Amount,
    string Direction,
    DateTime CreatedAt
);

// Raw account data; display labels and icons are chosen by each client.
public sealed record AccountItem(
    long AccountId,
    string AccountType,
    string CurrencyCode,
    string Iban,
    decimal Balance,
    string Status
);
