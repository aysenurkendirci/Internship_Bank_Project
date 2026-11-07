namespace Bank.Contracts.Finance;

/// <param name="Months">Analysed months as "yyyy-MM", oldest first; every series below is aligned with it.</param>
public sealed record CategoryTrendsResponse(
    IReadOnlyList<string> Months,
    IReadOnlyList<decimal> MonthlyTotals,
    IReadOnlyList<CategoryTrendResponse> Categories);

public sealed record CategoryTrendResponse(
    string Category,
    IReadOnlyList<decimal> MonthlySpending,
    decimal Total,
    decimal AverageMonthly,
    decimal Share,
    decimal? ChangeFromPreviousMonth,
    decimal SlopePerMonth);

/// <param name="Pattern">Fixed (same amount every time) or Variable (bill-like).</param>
/// <param name="IsTracked">Whether a recurring item with a matching name is already entered; untracked ones are candidates to add.</param>
public sealed record RecurringPaymentResponse(
    string Merchant,
    string Direction,
    string Pattern,
    decimal TypicalAmount,
    int TypicalDay,
    int MonthsObserved,
    DateOnly LastSeen,
    DateOnly NextExpected,
    string Category,
    bool IsTracked);

/// <param name="TimesTypical">How many times the typical amount of <paramref name="ComparedWith"/> this purchase is.</param>
public sealed record AnomalyResponse(
    DateOnly Date,
    string Description,
    decimal Amount,
    string Category,
    decimal TypicalAmount,
    decimal TimesTypical,
    string ComparedWith,
    double Score);

/// <param name="BasedOnMonths">Number of complete months of history the forecast used.</param>
public sealed record SpendingForecastResponse(
    string Method,
    int BasedOnMonths,
    IReadOnlyList<ForecastMonthResponse> Months,
    IReadOnlyList<ForecastBacktestResponse> Backtests);

public sealed record ForecastMonthResponse(string Month, decimal Expected, decimal Lower, decimal Upper);

public sealed record ForecastBacktestResponse(string Method, int Forecasts, double MeanAbsolutePercentageError, double IntervalCoverage);
