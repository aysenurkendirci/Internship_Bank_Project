namespace Bank.Contracts.Dashboard;

public sealed record SavingsGoalItem(
    long GoalId,
    string Title,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal ProgressPercent,
    string Status,
    DateTime CreatedAt
);
