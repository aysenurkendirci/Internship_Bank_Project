namespace Bank.Contracts.Cards;

public sealed record CardDetailResponse
{
    public long CardId { get; init; }
    public string CardNo { get; init; } = "";
    public string CardType { get; init; } = "";
    public bool IsVirtual { get; init; }
    public string Status { get; init; } = "";

    public long AccountId { get; init; }
    public string AccountType { get; init; } = "";
    public string Iban { get; init; } = "";
    public decimal AccountBalance { get; init; }

    public bool Contactless { get; init; }
    public bool OnlineUse { get; init; }
    public decimal DailyLimit { get; init; }
    public decimal MonthlyLimit { get; init; }

    public string OwnerFirstName { get; init; } = "";
    public string Membership { get; init; } = "";
}
