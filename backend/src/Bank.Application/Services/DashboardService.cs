using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
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

    public async Task<IReadOnlyList<SavingsGoalItem>> GetSavingsGoalsAsync(CancellationToken ct = default)
    {
        var rows = await _repo.GetSavingsGoalsAsync(_currentUser.UserId, ct);

        return rows.Select(g => new SavingsGoalItem(
            g.GOAL_ID,
            g.TITLE,
            g.TARGET_AMOUNT,
            g.CURRENT_AMOUNT,
            g.PROGRESS_PERCENT,
            g.STATUS,
            g.CREATED_AT
        )).ToList();
    }

    public async Task CreateSavingsGoalAsync(CreateSavingsGoalRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            throw new ArgumentException("Title is required.");

        if (req.TargetAmount <= 0)
            throw new ArgumentException("TargetAmount must be > 0.");

        await _repo.CreateSavingsGoalAsync(_currentUser.UserId, req, ct);
    }
}
