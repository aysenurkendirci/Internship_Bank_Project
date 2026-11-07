using Bank.Contracts.Finance;

namespace Bank.Application.Abstractions.Services;

public interface IProjectionService
{
    Task<ProjectionResponse> ProjectAsync(int months, CancellationToken ct = default);
}
