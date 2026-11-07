using Bank.Domain.Common;

namespace Bank.Domain.Analytics;

/// <param name="Score">Robust z-score of the amount; above <see cref="AnomalyDetector.Threshold"/> is flagged.</param>
/// <param name="TypicalAmount">The median amount of the group the entry was compared with.</param>
/// <param name="ComparedWith">The category used as baseline, or <see cref="AnomalyDetector.AllSpending"/>.</param>
public sealed record SpendingAnomaly(HistoryEntry Entry, double Score, decimal TypicalAmount, string ComparedWith)
{
    public decimal TimesTypical => TypicalAmount == 0 ? 0 : Math.Round(-Entry.Amount / TypicalAmount, 1);
}

/// <summary>
/// Flags purchases that are unusually large for the user, so they can be reviewed. The result is
/// an explanation ("8× your typical grocery spend"), not a judgement about the purchase.
/// </summary>
/// <remarks>
/// Method: the modified z-score of Iglewicz and Hoaglin, M = 0.6745 (x − median) / MAD, on the
/// logarithm of the amount, flagged when M exceeds 3.5.
/// <list type="bullet">
///   <item>Median and MAD rather than mean and standard deviation: a single 30 000 TRY purchase drags
///   the mean up far enough to make itself look normal; the median does not move.</item>
///   <item>Log scale: spending is right-skewed, and going from 100 to 200 is a bigger deviation than
///   going from 10 000 to 10 100.</item>
///   <item>Each entry is compared with its own category when that category has at least
///   <see cref="MinimumGroupSize"/> entries; rarer categories are compared with all spending.</item>
///   <item>Recurring payments are excluded: rent is large but expected.</item>
///   <item>Only unusually large amounts are flagged; unusually small ones tell the user nothing.</item>
/// </list>
/// The baseline is the whole history passed in, so it describes the user's habits rather than only the past.
/// </remarks>
public static class AnomalyDetector
{
    public const double Threshold = 3.5;
    public const int MinimumGroupSize = 10;
    public const string AllSpending = "Tüm harcamalar";

    // Scales the MAD so it estimates the standard deviation of normally distributed data.
    private const double MadToSigma = 0.6745;

    // Fallback when more than half the values are identical and the MAD is zero.
    private const double MeanAbsoluteDeviationToSigma = 1.2533;

    /// <param name="threshold">
    /// Exposed for sensitivity analysis. Lower values catch smaller deviations at the cost of more
    /// false alarms; the default is the value recommended in the literature, not one fitted to data.
    /// </param>
    public static IReadOnlyList<SpendingAnomaly> Detect(IReadOnlyList<HistoryEntry> history, double threshold = Threshold)
    {
        var recurring = RecurringPaymentDetector.Detect(history)
            .Where(p => !p.IsIncome)
            .Select(p => TextFolding.Fold(p.Merchant))
            .ToHashSet();

        var candidates = history
            .Where(e => e.IsOutflow && !recurring.Contains(TextFolding.Fold(e.Description)))
            .ToList();

        if (candidates.Count < MinimumGroupSize)
            return [];

        var overall = Baseline.Of(candidates);
        var byCategory = candidates
            .GroupBy(e => e.CategoryOrDefault)
            .Where(g => g.Count() >= MinimumGroupSize)
            .ToDictionary(g => g.Key, g => Baseline.Of(g.ToList()));

        return candidates
            .Select(entry =>
            {
                var hasOwn = byCategory.TryGetValue(entry.CategoryOrDefault, out var own);
                var baseline = hasOwn ? own! : overall;
                return (entry, baseline, comparedWith: hasOwn ? entry.CategoryOrDefault : AllSpending);
            })
            .Where(x => x.baseline.Spread > 0)
            .Select(x => new SpendingAnomaly(
                x.entry,
                Math.Round(x.baseline.Score(x.entry), 2),
                Math.Round((decimal)Math.Exp(x.baseline.LogMedian), 2),
                x.comparedWith))
            .Where(a => a.Score > threshold)
            .OrderByDescending(a => a.Score)
            .ToList();
    }

    private sealed record Baseline(double LogMedian, double Spread)
    {
        public static Baseline Of(IReadOnlyList<HistoryEntry> entries)
        {
            var logs = entries.Select(e => Math.Log((double)-e.Amount)).ToList();
            var median = Statistics.Median(logs);
            var mad = Statistics.MedianAbsoluteDeviation(logs);

            if (mad == 0)
            {
                // More than half the amounts are identical; the mean absolute deviation still sees the rest.
                var meanAbsoluteDeviation = logs.Average(l => Math.Abs(l - median));
                return new Baseline(median, meanAbsoluteDeviation * MeanAbsoluteDeviationToSigma * MadToSigma);
            }

            return new Baseline(median, mad);
        }

        public double Score(HistoryEntry entry) => MadToSigma * (Math.Log((double)-entry.Amount) - LogMedian) / Spread;
    }
}
