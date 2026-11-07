using Bank.Contracts.Finance;

namespace Bank.Application.Abstractions.Services;

public interface IScenarioService
{
    Task<ScenarioComparisonResponse> CompareInstallmentsAsync(InstallmentScenarioRequest request, CancellationToken ct = default);
}
