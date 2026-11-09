using Bank.Contracts.Dashboard;

namespace Bank.Application.Abstractions.Repositories;

/// <summary>Read side of the home screen: returns the response shape straight from SQL, as queries may (ADR-0006).</summary>
public interface IDashboardRepository
{
    Task<DashboardResponse> GetDashboardAsync(long userId, CancellationToken ct = default);
}
