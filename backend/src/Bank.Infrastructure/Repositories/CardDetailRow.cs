namespace Bank.Infrastructure.Repositories;

public sealed class CardDetailRow
{
    public decimal CardId { get; set; }
    public decimal AccountId { get; set; }

    public string CardNo { get; set; } = "";
    public string CardType { get; set; } = "";
    public string IsVirtual { get; set; } = "";
    public string Status { get; set; } = "";

    public string AccountType { get; set; } = "";
    public string Iban { get; set; } = "";
    public decimal AccountBalance { get; set; }

    public string Contactless { get; set; } = "";
    public string OnlineUse { get; set; } = "";

    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }

    public string OwnerFirstName { get; set; } = "";
    public string Membership { get; set; } = "";
}
