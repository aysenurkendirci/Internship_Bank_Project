using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Services;
using Bank.Contracts.Dashboard;

namespace Bank.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _repo;

    public DashboardService(IDashboardRepository repo) => _repo = repo;

    public Task<DashboardResponse> GetDashboardAsync(long userId, CancellationToken ct = default)
        => _repo.GetDashboardAsync(userId, ct);
}
