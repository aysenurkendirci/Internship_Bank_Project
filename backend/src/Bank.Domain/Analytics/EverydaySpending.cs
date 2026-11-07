using Bank.Domain.Common;

namespace Bank.Domain.Analytics;

/// <summary>
/// Monthly totals of routine variable spending: groceries, eating out, transport and the like.
/// Recurring payments are left out because the projection already knows them exactly, and one-off
/// anomalies are left out because they would distort a forecast of the usual month.
/// </summary>
public static class EverydaySpending
{
    public static IReadOnlyList<decimal> MonthlyTotals(IReadOnlyList<HistoryEntry> history, YearMonth first, int months)
    {
        var recurring = RecurringPaymentDetector.Detect(history)
            .Where(p => !p.IsIncome)
            .Select(p => TextFolding.Fold(p.Merchant))
            .ToHashSet();
        var anomalies = AnomalyDetector.Detect(history).Select(a => a.Entry).ToHashSet();

        var routine = history
            .Where(e => e.IsOutflow && !anomalies.Contains(e) && !recurring.Contains(TextFolding.Fold(e.Description)))
            .ToList();

        return Enumerable.Range(0, months)
            .Select(first.AddMonths)
            .Select(m => -routine.Where(e => YearMonth.Of(e.Date) == m).Sum(e => e.Amount))
            .ToList();
    }
}
