using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Contracts.Dashboard;

namespace Bank.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _repo;
    private readonly ICurrentUser _currentUser;

    public DashboardService(IDashboardRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default)
        => _repo.GetDashboardAsync(userId: _currentUser.UserId, ct);

    public async Task<IReadOnlyList<TransactionItem>> GetRecentTransactionsAsync(CancellationToken ct = default)
    {
        var dash = await GetDashboardAsync(ct);
        return dash.RecentTransactions;
    }
}
