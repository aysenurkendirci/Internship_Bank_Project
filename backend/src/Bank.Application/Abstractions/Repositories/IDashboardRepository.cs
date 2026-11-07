using Bank.Contracts.Dashboard;

namespace Bank.Application.Abstractions.Repositories;

public interface IDashboardRepository
{
    Task<DashboardResponse> GetDashboardAsync(long userId, CancellationToken ct = default);
}
