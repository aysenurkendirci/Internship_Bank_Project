using Bank.Application.Abstractions.Services;
using Bank.Contracts.Finance;
using Bank.Domain.Scenarios;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Services.Finance;

public sealed class ScenarioService : IScenarioService
{
    private readonly ProjectionInputLoader _inputs;

    public ScenarioService(ProjectionInputLoader inputs) => _inputs = inputs;

    public async Task<ScenarioComparisonResponse> CompareInstallmentsAsync(
        InstallmentScenarioRequest request, CancellationToken ct = default)
    {
        var current = await _inputs.LoadAsync(request.Months, ct);

        // The card must be one of the user's own; ScenarioComparer rejects any other id.
        var scenario = new PurchaseScenario(
            request.CreditCardId!.Value,
            request.Description,
            request.PurchaseDate!.Value,
            request.Options
                .Select(o => new PurchaseOption(o.InstallmentCount, new Money(o.TotalAmount, ProjectionInputLoader.ProjectionCurrency)))
                .ToList());

        var comparison = ScenarioComparer.Compare(current, scenario);

        return new ScenarioComparisonResponse(
            comparison.From,
            comparison.To,
            ToResponse(comparison.Baseline),
            comparison.Options.Select(o => new ScenarioOptionResponse(
                o.Option.InstallmentCount,
                o.Option.TotalAmount.Amount,
                o.Installments.Select(i => new InstallmentResponse(i.Number, i.StatementDate, i.DueDate, i.Amount.Amount)).ToList(),
                ToResponse(o.Metrics))).ToList());
    }

    private static ScenarioMetricsResponse ToResponse(ScenarioMetrics m)
        => new(
            m.LowestBalance.Amount,
            m.LowestBalanceDate,
            m.MonthsBelowZero,
            m.DeficitMonths,
            m.PeakBurdenRatio,
            m.PeakMonthlyOutflow.Amount,
            m.ClosingBalance.Amount,
            m.Months.Select(ProjectionMapper.ToResponse).ToList());
}
