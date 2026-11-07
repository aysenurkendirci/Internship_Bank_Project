using Bank.Contracts.Finance;
using Bank.Domain.Scenarios;
using Bank.Domain.ValueObjects;
using MediatR;

namespace Bank.Application.Features.Planning;

/// <summary>
/// Compares installment options for a planned card purchase against the user's current outlook.
/// A query, not a command: the purchase only exists for the duration of the calculation.
/// </summary>
public sealed record CompareInstallmentOptionsQuery(InstallmentScenarioRequest Scenario) : IRequest<ScenarioComparisonResponse>;

public sealed class CompareInstallmentOptionsQueryHandler
    : IRequestHandler<CompareInstallmentOptionsQuery, ScenarioComparisonResponse>
{
    private readonly ProjectionInputLoader _inputs;

    public CompareInstallmentOptionsQueryHandler(ProjectionInputLoader inputs) => _inputs = inputs;

    public async Task<ScenarioComparisonResponse> Handle(CompareInstallmentOptionsQuery query, CancellationToken ct)
    {
        var request = query.Scenario;
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
