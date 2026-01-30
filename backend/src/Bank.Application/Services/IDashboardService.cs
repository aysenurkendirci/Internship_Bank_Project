using Bank.Contracts.Dashboard;

namespace Bank.Application.Abstractions.Services;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(long userId, CancellationToken ct = default);
}
