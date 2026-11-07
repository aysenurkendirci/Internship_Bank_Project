using Bank.Contracts.Finance;

namespace Bank.Application.Abstractions.Services;

public interface IInstallmentPlanService
{
    Task<IReadOnlyList<InstallmentPlanResponse>> ListAsync(CancellationToken ct = default);
    Task<InstallmentPlanResponse> GetAsync(long id, CancellationToken ct = default);
    Task<InstallmentPlanResponse> CreateAsync(InstallmentPlanRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
