namespace Bank.Infrastructure.Repositories;

public sealed record CardDetailRow(
    long CardId,
    string CardNo,
    string CardType,
    string IsVirtual,
    string Status,
    long AccountId,
    string AccountType,
    string Iban,
    decimal AccountBalance,
    string Contactless,
    string OnlineUse,
    decimal DailyLimit,
    decimal MonthlyLimit,
    string OwnerFirstName,
    string Membership
);
