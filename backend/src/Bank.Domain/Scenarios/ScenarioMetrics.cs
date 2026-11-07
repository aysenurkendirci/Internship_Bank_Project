using Bank.Domain.Projection;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Scenarios;

/// <summary>Facts about one projected future, used to compare options side by side.</summary>
/// <param name="MonthsBelowZero">Months in which the balance drops below zero at any point.</param>
/// <param name="DeficitMonths">Months whose outflows exceed income.</param>
/// <param name="PeakMonthlyOutflow">The largest single-month outflow over the horizon.</param>
public sealed record ScenarioMetrics(
    Money LowestBalance,
    DateOnly? LowestBalanceDate,
    int MonthsBelowZero,
    int DeficitMonths,
    decimal? PeakBurdenRatio,
    Money PeakMonthlyOutflow,
    Money ClosingBalance,
    IReadOnlyList<MonthlyCashFlow> Months)
{
    public static ScenarioMetrics From(CashFlowProjection projection)
    {
        var months = projection.Months;
        var lowest = months.MinBy(m => m.LowestBalance.Amount)!;

        return new ScenarioMetrics(
            lowest.LowestBalance,
            lowest.LowestBalanceDate,
            months.Count(m => m.BalanceGoesNegative),
            months.Count(m => m.IsDeficit),
            months.Max(m => m.BurdenRatio),
            months.MaxBy(m => m.Outflows.Amount)!.Outflows,
            months[^1].ClosingBalance,
            months);
    }
}
