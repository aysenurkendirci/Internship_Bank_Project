using Bank.Domain.Common;

namespace Bank.Domain.Analytics;

/// <param name="MonthlySpending">Spending per month, aligned with <see cref="CategoryTrendReport.Months"/>; zero when nothing was spent.</param>
/// <param name="Share">Fraction of all spending in the period that went to this category.</param>
/// <param name="ChangeFromPreviousMonth">Last month relative to the one before (0.25 = +25%); null when the earlier month was zero.</param>
/// <param name="SlopePerMonth">Least-squares trend in TRY per month over the whole period.</param>
public sealed record CategoryTrend(
    string Category,
    IReadOnlyList<decimal> MonthlySpending,
    decimal Total,
    decimal AverageMonthly,
    decimal Share,
    decimal? ChangeFromPreviousMonth,
    decimal SlopePerMonth);

public sealed record CategoryTrendReport(
    IReadOnlyList<YearMonth> Months,
    IReadOnlyList<decimal> MonthlyTotals,
    IReadOnlyList<CategoryTrend> Categories);

/// <summary>
/// Monthly spending per category over a period, largest category first. Only outflows count as
/// spending; income is left out.
/// </summary>
public static class CategoryTrendAnalyzer
{
    public static CategoryTrendReport Analyze(IEnumerable<HistoryEntry> history, YearMonth first, int months)
    {
        if (months < 1)
            throw new DomainException("En az bir ay analiz edilmelidir.");

        var period = Enumerable.Range(0, months).Select(first.AddMonths).ToList();
        var last = period[^1];

        var spending = history
            .Where(e => e.IsOutflow && YearMonth.Of(e.Date) >= first && YearMonth.Of(e.Date) <= last)
            .ToList();

        var monthlyTotals = period
            .Select(m => -spending.Where(e => YearMonth.Of(e.Date) == m).Sum(e => e.Amount))
            .ToList();
        var grandTotal = monthlyTotals.Sum();

        var categories = spending
            .GroupBy(e => e.CategoryOrDefault)
            .Select(group =>
            {
                var perMonth = period
                    .Select(m => -group.Where(e => YearMonth.Of(e.Date) == m).Sum(e => e.Amount))
                    .ToList();
                var total = perMonth.Sum();

                return new CategoryTrend(
                    group.Key,
                    perMonth,
                    total,
                    Math.Round(total / months, 2),
                    grandTotal == 0 ? 0 : Math.Round(total / grandTotal, 4),
                    ChangeOfLastMonth(perMonth),
                    Math.Round((decimal)Statistics.Slope(perMonth.Select(v => (double)v).ToList()), 2));
            })
            .OrderByDescending(c => c.Total)
            .ToList();

        return new CategoryTrendReport(period, monthlyTotals, categories);
    }

    private static decimal? ChangeOfLastMonth(IReadOnlyList<decimal> perMonth)
    {
        if (perMonth.Count < 2 || perMonth[^2] == 0)
            return null;

        return Math.Round((perMonth[^1] - perMonth[^2]) / perMonth[^2], 4);
    }
}
