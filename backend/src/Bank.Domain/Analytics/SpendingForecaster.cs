using Bank.Domain.Common;

namespace Bank.Domain.Analytics;

public enum ForecastMethod
{
    /// <summary>Next month equals the last month.</summary>
    Naive,

    /// <summary>Average of the last three months.</summary>
    MovingAverage,

    /// <summary>Least-squares line through the last six months, extended forward.</summary>
    LinearTrend
}

/// <param name="Lower">Lower bound of the 95% prediction interval, never below zero.</param>
public sealed record MonthlyForecast(YearMonth Month, decimal Expected, decimal Lower, decimal Upper);

/// <param name="MeanAbsolutePercentageError">Average |actual − forecast| / actual over all one-step forecasts.</param>
/// <param name="IntervalCoverage">Share of actual values that fell inside the 95% interval.</param>
public sealed record BacktestResult(ForecastMethod Method, int Forecasts, double MeanAbsolutePercentageError, double IntervalCoverage);

public sealed record SpendingForecast(
    ForecastMethod Method,
    IReadOnlyList<MonthlyForecast> Months,
    IReadOnlyList<BacktestResult> Backtests);

/// <summary>
/// Forecasts monthly spending with deliberately simple, explainable methods, and chooses between them by
/// how well each would have done on the user's own past (rolling-origin backtest).
/// </summary>
/// <remarks>
/// Backtest: for every month after the first <see cref="MinimumHistory"/>, each method forecasts it from
/// the months before it only, and the error is recorded. The method with the lowest mean absolute
/// percentage error is used.
/// Interval: expected ± 1.96 × RMSE of those one-step errors × √h for h months ahead. The √h widening
/// assumes independent errors, an approximation that is reported together with the measured coverage.
/// </remarks>
public static class SpendingForecaster
{
    public const int MinimumHistory = 6;
    private const int MovingAverageWindow = 3;
    private const int TrendWindow = 6;
    private const double Z95 = 1.96;

    public static IReadOnlyList<BacktestResult> Backtest(IReadOnlyList<decimal> monthlySpending)
        => Enum.GetValues<ForecastMethod>()
            .Select(method =>
            {
                var errors = OneStepErrors(monthlySpending, method);
                var rmse = Rmse(errors);
                var inside = errors.Count(e => Math.Abs(e.Actual - e.Predicted) <= Z95 * rmse);

                return new BacktestResult(
                    method,
                    errors.Count,
                    errors.Count == 0 ? double.NaN : errors.Average(e => Math.Abs(e.Actual - e.Predicted) / e.Actual),
                    errors.Count == 0 ? double.NaN : (double)inside / errors.Count);
            })
            .ToList();

    /// <param name="monthlySpending">Complete months in order, oldest first; positive amounts.</param>
    /// <param name="lastMonth">The month of the last value in <paramref name="monthlySpending"/>.</param>
    /// <param name="method">Forces a method; by default the one with the lowest backtest error is chosen.</param>
    public static SpendingForecast Forecast(
        IReadOnlyList<decimal> monthlySpending, YearMonth lastMonth, int horizon, ForecastMethod? method = null)
    {
        if (monthlySpending.Count < MinimumHistory)
            throw new DomainException($"Tahmin için en az {MinimumHistory} aylık geçmiş gerekir.");
        if (horizon is < 1 or > 12)
            throw new DomainException("Tahmin süresi 1 ile 12 ay arasında olmalıdır.");

        var backtests = Backtest(monthlySpending);
        var chosen = method ?? backtests.MinBy(b => b.MeanAbsolutePercentageError)!.Method;
        var rmse = Rmse(OneStepErrors(monthlySpending, chosen));
        var series = monthlySpending.Select(v => (double)v).ToList();

        var months = Enumerable.Range(1, horizon)
            .Select(h =>
            {
                var expected = Math.Max(0, Predict(series, chosen, h));
                var margin = Z95 * rmse * Math.Sqrt(h);
                return new MonthlyForecast(
                    lastMonth.AddMonths(h),
                    Round(expected),
                    Round(Math.Max(0, expected - margin)),
                    Round(expected + margin));
            })
            .ToList();

        return new SpendingForecast(chosen, months, backtests);
    }

    private static double Predict(IReadOnlyList<double> history, ForecastMethod method, int stepsAhead) => method switch
    {
        ForecastMethod.Naive => history[^1],
        ForecastMethod.MovingAverage => history.TakeLast(MovingAverageWindow).Average(),
        ForecastMethod.LinearTrend => TrendForecast(history.TakeLast(TrendWindow).ToList(), stepsAhead),
        _ => throw new InvalidOperationException($"Unhandled forecast method {method}.")
    };

    private static double TrendForecast(IReadOnlyList<double> window, int stepsAhead)
    {
        var slope = Statistics.Slope(window);
        var meanX = (window.Count - 1) / 2.0;
        var intercept = window.Average() - slope * meanX;
        return intercept + slope * (window.Count - 1 + stepsAhead);
    }

    private static List<(double Actual, double Predicted)> OneStepErrors(IReadOnlyList<decimal> spending, ForecastMethod method)
    {
        var series = spending.Select(v => (double)v).ToList();
        var errors = new List<(double, double)>();

        for (var t = MinimumHistory; t < series.Count; t++)
        {
            if (series[t] <= 0)
                continue;
            errors.Add((series[t], Predict(series.Take(t).ToList(), method, stepsAhead: 1)));
        }

        return errors;
    }

    private static double Rmse(IReadOnlyCollection<(double Actual, double Predicted)> errors)
        => errors.Count == 0 ? 0 : Math.Sqrt(errors.Average(e => Math.Pow(e.Actual - e.Predicted, 2)));

    private static decimal Round(double value) => Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
}
