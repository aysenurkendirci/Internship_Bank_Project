using Bank.Domain.Common;

namespace Bank.Domain.Analytics;

public enum RecurringPattern
{
    /// <summary>Same amount every time, like a subscription or rent.</summary>
    Fixed,

    /// <summary>Same merchant and day every month but a varying amount, like a utility bill.</summary>
    Variable
}

/// <param name="Merchant">The description as it last appeared on a statement.</param>
/// <param name="TypicalAmount">Median absolute amount.</param>
/// <param name="NextExpected">The next month's date on the typical day.</param>
public sealed record RecurringPayment(
    string Merchant,
    bool IsIncome,
    RecurringPattern Pattern,
    decimal TypicalAmount,
    int TypicalDay,
    int MonthsObserved,
    DateOnly LastSeen,
    DateOnly NextExpected,
    string Category);

/// <summary>
/// Finds payments that repeat monthly in the user's history, so subscriptions and bills the user never
/// entered can be suggested for tracking.
/// </summary>
/// <remarks>
/// A merchant counts as recurring when, under one direction (in or out):
/// <list type="bullet">
///   <item>it appears in at least <see cref="MinimumMonths"/> months, at most once per month;</item>
///   <item>it is present in at least 75% of the months between its first and last appearance;</item>
///   <item>its day of month is consistent (median absolute deviation of at most 3 days, which
///   tolerates weekend shifts).</item>
/// </list>
/// Amounts within ±5% of the median make it <see cref="RecurringPattern.Fixed"/>, otherwise
/// <see cref="RecurringPattern.Variable"/>. Merchant names are compared after Turkish folding, so
/// "MİGROS" and "MIGROS" are the same merchant.
/// </remarks>
public static class RecurringPaymentDetector
{
    public const int MinimumMonths = 3;
    private const double MinimumCoverage = 0.75;
    private const double MaxDayDeviation = 3;
    private const double FixedAmountTolerance = 0.05;

    public static IReadOnlyList<RecurringPayment> Detect(IEnumerable<HistoryEntry> history)
        => history
            .GroupBy(e => (Merchant: TextFolding.Fold(e.Description), IsIncome: !e.IsOutflow))
            .Select(g => Evaluate(g.OrderBy(e => e.Date).ToList(), g.Key.IsIncome))
            .OfType<RecurringPayment>()
            .OrderBy(p => p.TypicalDay)
            .ToList();

    private static RecurringPayment? Evaluate(IReadOnlyList<HistoryEntry> entries, bool isIncome)
    {
        var months = entries.Select(e => YearMonth.Of(e.Date)).ToList();
        var distinctMonths = months.Distinct().Count();

        if (distinctMonths < MinimumMonths || distinctMonths != entries.Count)
            return null;

        var span = (months[^1].Year - months[0].Year) * 12 + months[^1].Month - months[0].Month + 1;
        if ((double)distinctMonths / span < MinimumCoverage)
            return null;

        var days = entries.Select(e => (double)e.Date.Day).ToList();
        if (Statistics.MedianAbsoluteDeviation(days) > MaxDayDeviation)
            return null;

        var amounts = entries.Select(e => (double)Math.Abs(e.Amount)).ToList();
        var typicalAmount = Statistics.Median(amounts);
        var pattern = amounts.All(a => Math.Abs(a - typicalAmount) <= typicalAmount * FixedAmountTolerance)
            ? RecurringPattern.Fixed
            : RecurringPattern.Variable;

        var typicalDay = (int)Math.Round(Statistics.Median(days));
        var nextMonth = months[^1].AddMonths(1);
        var last = entries[^1];

        return new RecurringPayment(
            last.Description,
            isIncome,
            pattern,
            Math.Round((decimal)typicalAmount, 2),
            typicalDay,
            distinctMonths,
            last.Date,
            new DateOnly(nextMonth.Year, nextMonth.Month, Math.Min(typicalDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month))),
            entries.GroupBy(e => e.CategoryOrDefault).MaxBy(c => c.Count())!.Key);
    }
}
