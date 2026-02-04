namespace Bank.Contracts.Accounts;

public sealed record AccountDetailResponse(
    long AccountId,
    string Type,
    string Iban,
    decimal Balance,
    string Status,
    DateTime CreatedAt,
    string OwnerFirstName,
    string Membership
);
