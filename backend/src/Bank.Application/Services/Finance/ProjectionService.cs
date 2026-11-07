using Bank.Application.Abstractions.Services;
using Bank.Contracts.Finance;
using Bank.Domain.Projection;

namespace Bank.Application.Services.Finance;

/// <summary>
/// Runs <see cref="CashFlowProjector"/> on the current user's data. All projection rules live in
/// the domain; this class only gathers inputs and maps the result.
/// </summary>
public sealed class ProjectionService : IProjectionService
{
    private readonly ProjectionInputLoader _inputs;

    public ProjectionService(ProjectionInputLoader inputs) => _inputs = inputs;

    public async Task<ProjectionResponse> ProjectAsync(int months, CancellationToken ct = default)
    {
        var projection = CashFlowProjector.Project(await _inputs.LoadAsync(months, ct));

        return new ProjectionResponse(
            projection.From,
            projection.To,
            projection.OpeningBalance.Currency.Code,
            projection.OpeningBalance.Amount,
            projection.Months.Select(ProjectionMapper.ToResponse).ToList(),
            projection.Entries.Select(ProjectionMapper.ToResponse).ToList(),
            projection.ExcludedItems);
    }
}
