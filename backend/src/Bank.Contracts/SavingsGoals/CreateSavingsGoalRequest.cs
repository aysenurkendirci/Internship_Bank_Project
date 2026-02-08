namespace Bank.Contracts.SavingsGoals;

public sealed record CreateSavingsGoalRequest
{
    public string Title { get; init; } = "";
    public decimal TargetAmount { get; init; }
}
