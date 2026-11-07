using Bank.Domain.Cards;

namespace Bank.Application.Abstractions.Repositories;

/// <summary>
/// Every method is scoped to <c>userId</c>. Plans cannot be edited after purchase, so unlike the other
/// finance repositories there is no update and no row version is surfaced.
/// </summary>
public interface IInstallmentPlanRepository
{
    Task<IReadOnlyList<InstallmentPlan>> ListAsync(long userId, CancellationToken ct = default);
    Task<InstallmentPlan?> GetAsync(long userId, long id, CancellationToken ct = default);
    Task<InstallmentPlan> AddAsync(long userId, InstallmentPlan plan, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(long userId, long id, CancellationToken ct = default);
}
