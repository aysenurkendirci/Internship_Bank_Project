using Bank.Contracts.Dashboard;

namespace Bank.Application.Abstractions.Repositories;

public interface IDashboardRepository
{
    Task<DashboardResponse> GetDashboardAsync(long userId, CancellationToken ct = default);

    Task<IReadOnlyList<SavingsGoalRow>> GetSavingsGoalsAsync(long userId, CancellationToken ct = default);
   
    Task CreateSavingsGoalAsync(long userId, CreateSavingsGoalRequest req, CancellationToken ct = default);

    Task AddGoalContributionAsync(long goalId, decimal amount, CancellationToken ct);
Task DeleteSavingsGoalAsync(long goalId, CancellationToken ct);

}

public sealed class SavingsGoalRow
{
    public long GOAL_ID { get; set; }
    public string TITLE { get; set; } = "";
    public decimal TARGET_AMOUNT { get; set; }
    public decimal CURRENT_AMOUNT { get; set; }
    public decimal PROGRESS_PERCENT { get; set; }
    public string STATUS { get; set; } = "";
    public DateTime CREATED_AT { get; set; }
}
