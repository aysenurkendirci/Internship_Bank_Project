using Bank.Contracts.Dashboard;

namespace Bank.Application.Services;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TransactionItem>> GetRecentTransactionsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SavingsGoalItem>> GetSavingsGoalsAsync(CancellationToken ct = default);
    Task CreateSavingsGoalAsync(CreateSavingsGoalRequest req, CancellationToken ct = default);
}
