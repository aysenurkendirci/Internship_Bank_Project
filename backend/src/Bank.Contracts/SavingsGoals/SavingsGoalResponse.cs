namespace Bank.Contracts.Dashboard;

public sealed record SavingsGoalResponse
{
    public long GoalId { get; init; }
    public string Title { get; init; } = "";
    public decimal TargetAmount { get; init; }
    public decimal CurrentAmount { get; init; }
    public decimal ProgressPercent { get; init; }
    public string Status { get; init; } = "";
    public DateTime CreatedAt { get; init; }
}
