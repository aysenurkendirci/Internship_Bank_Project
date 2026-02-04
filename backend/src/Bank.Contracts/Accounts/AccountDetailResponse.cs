namespace Bank.Contracts.Cards;

public sealed record CardDetailResponse(
    long CardId,
    string CardNo,
    string CardType,
    bool IsVirtual,
    string Status,
    long AccountId,
    string AccountType,
    string Iban,
    decimal AccountBalance,
    bool Contactless,
    bool OnlineUse,
    decimal DailyLimit,
    decimal MonthlyLimit,
    string OwnerFirstName,
    string Membership
);
