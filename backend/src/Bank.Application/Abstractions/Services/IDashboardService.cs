using Bank.Contracts.Dashboard;

namespace Bank.Application.Abstractions.Services;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TransactionItem>> GetRecentTransactionsAsync(CancellationToken ct = default);
}
