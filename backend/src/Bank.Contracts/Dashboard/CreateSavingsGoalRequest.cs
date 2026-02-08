namespace Bank.Contracts.Dashboard;

public sealed record CreateSavingsGoalRequest
{
    public string Title { get; init; } = "";
    public decimal TargetAmount { get; init; }
}
