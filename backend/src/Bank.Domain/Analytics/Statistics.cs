namespace Bank.Domain.Analytics;

/// <summary>Small descriptive statistics used across the analytics. Kept here so each is tested once.</summary>
public static class Statistics
{
    public static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            throw new ArgumentException("Median of an empty set is undefined.", nameof(values));

        var sorted = values.Order().ToArray();
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
    }

    /// <summary>Median absolute deviation: the median distance from the median. Unaffected by a few extreme values.</summary>
    public static double MedianAbsoluteDeviation(IReadOnlyList<double> values)
    {
        var median = Median(values);
        return Median(values.Select(v => Math.Abs(v - median)).ToList());
    }

    public static double StandardDeviation(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
            return 0;

        var mean = values.Average();
        return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1));
    }

    /// <summary>Ordinary least squares slope of <paramref name="values"/> against their index (0, 1, 2, ...).</summary>
    public static double Slope(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
            return 0;

        var meanX = (values.Count - 1) / 2.0;
        var meanY = values.Average();
        var covariance = values.Select((y, x) => (x - meanX) * (y - meanY)).Sum();
        var variance = values.Select((_, x) => (x - meanX) * (x - meanX)).Sum();
        return covariance / variance;
    }
}
