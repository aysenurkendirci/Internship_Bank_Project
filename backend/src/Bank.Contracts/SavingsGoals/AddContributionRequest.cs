namespace Bank.Contracts.SavingsGoals;

public sealed record AddContributionRequest
{
    public decimal Amount { get; init; }
    // şimdilik sadece hedefe para ekleme (account'tan düşme yok)
    // DB’ne uygun şekilde ileride accountId ekleriz.
}
