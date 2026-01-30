namespace Bank.Contracts.Dashboard;

public sealed record DashboardResponse(
    UserSummary User,
    decimal TotalWealth,
    decimal WealthChangeRate,
    IReadOnlyList<CardItem> Cards,
    IReadOnlyList<TransactionItem> RecentTransactions
);

public sealed record UserSummary(
    long UserId,
    string FirstName,
    string Membership
);

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
