using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Application.Common;
using Bank.Contracts.Finance;
using Bank.Domain.Analytics;
using Bank.Domain.Common;

namespace Bank.Application.Services.Finance;

/// <summary>
/// Loads the user's transaction history and runs the domain analytics on it. Trends and forecasts use
/// complete months only: comparing a half-finished month with a full one would report a false drop.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    // Long enough for the detectors to learn the user's habits, short enough to reflect current ones.
    private const int BaselineMonths = 12;
    private const int MaxForecastHistoryMonths = 24;

    private readonly ITransactionHistoryRepository _history;
    private readonly IRecurringCashFlowRepository _recurringItems;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public AnalyticsService(
        ITransactionHistoryRepository history,
        IRecurringCashFlowRepository recurringItems,
        ICurrentUser currentUser,
        TimeProvider time)
    {
        _history = history;
        _recurringItems = recurringItems;
        _currentUser = currentUser;
        _time = time;
    }

    private YearMonth LastCompleteMonth => YearMonth.Of(BusinessDate.Today(_time)).AddMonths(-1);

    public async Task<CategoryTrendsResponse> CategoryTrendsAsync(int months, CancellationToken ct = default)
    {
        var first = LastCompleteMonth.AddMonths(-(months - 1));
        var history = await LoadAsync(first.FirstDay, LastCompleteMonth.LastDay, ct);

        var report = CategoryTrendAnalyzer.Analyze(history, first, months);

        return new CategoryTrendsResponse(
            report.Months.Select(m => m.ToString()).ToList(),
            report.MonthlyTotals,
            report.Categories.Select(c => new CategoryTrendResponse(
                c.Category, c.MonthlySpending, c.Total, c.AverageMonthly, c.Share, c.ChangeFromPreviousMonth, c.SlopePerMonth)).ToList());
    }

    public async Task<IReadOnlyList<RecurringPaymentResponse>> RecurringPaymentsAsync(CancellationToken ct = default)
    {
        var history = await LoadBaselineAsync(ct);
        var tracked = (await _recurringItems.ListAsync(_currentUser.UserId, kind: null, ct))
            .Select(i => TextFolding.Fold(i.Value.Name))
            .ToList();

        return RecurringPaymentDetector.Detect(history)
            .Select(p => new RecurringPaymentResponse(
                p.Merchant,
                p.IsIncome ? "Inbound" : "Outbound",
                p.Pattern.ToString(),
                p.TypicalAmount,
                p.TypicalDay,
                p.MonthsObserved,
                p.LastSeen,
                p.NextExpected,
                p.Category,
                IsTracked(p.Merchant, tracked)))
            .ToList();
    }

    public async Task<IReadOnlyList<AnomalyResponse>> AnomaliesAsync(int months, CancellationToken ct = default)
    {
        // The baseline covers a full year even when only recent months are reported,
        // so "unusual" is judged against the user's habits rather than a few weeks.
        var history = await LoadBaselineAsync(ct);
        var since = YearMonth.Of(BusinessDate.Today(_time)).AddMonths(-(months - 1)).FirstDay;

        return AnomalyDetector.Detect(history)
            .Where(a => a.Entry.Date >= since)
            .OrderByDescending(a => a.Entry.Date)
            .Select(a => new AnomalyResponse(
                a.Entry.Date,
                a.Entry.Description,
                a.Entry.Amount,
                a.Entry.CategoryOrDefault,
                a.TypicalAmount,
                a.TimesTypical,
                a.ComparedWith,
                a.Score))
            .ToList();
    }

    public async Task<SpendingForecastResponse> ForecastAsync(int months, CancellationToken ct = default)
    {
        var last = LastCompleteMonth;
        var history = await LoadAsync(last.AddMonths(-(MaxForecastHistoryMonths - 1)).FirstDay, last.LastDay, ct);

        // Start at the first month with any data, so a short history is not padded with empty months.
        var first = history.Count == 0 ? last : YearMonth.Of(history.Min(e => e.Date));
        var count = (last.Year - first.Year) * 12 + last.Month - first.Month + 1;
        var series = EverydaySpending.MonthlyTotals(history, first, count);

        var forecast = SpendingForecaster.Forecast(series, last, months);

        return new SpendingForecastResponse(
            forecast.Method.ToString(),
            series.Count,
            forecast.Months.Select(m => new ForecastMonthResponse(m.Month.ToString(), m.Expected, m.Lower, m.Upper)).ToList(),
            forecast.Backtests.Select(b => new ForecastBacktestResponse(
                b.Method.ToString(), b.Forecasts, Math.Round(b.MeanAbsolutePercentageError, 4), Math.Round(b.IntervalCoverage, 4))).ToList());
    }

    /// <summary>
    /// Names typed by the user rarely match statement text exactly ("Netflix" vs "NETFLIX.COM"), so a
    /// payment counts as tracked when either folded name contains the other.
    /// </summary>
    private static bool IsTracked(string merchant, IEnumerable<string> trackedNames)
    {
        var folded = TextFolding.Fold(merchant);
        return trackedNames.Any(name => name.Length > 0 && (folded.Contains(name) || name.Contains(folded)));
    }

    private Task<IReadOnlyList<HistoryEntry>> LoadBaselineAsync(CancellationToken ct)
    {
        var today = BusinessDate.Today(_time);
        return LoadAsync(today.AddMonths(-BaselineMonths), today, ct);
    }

    private async Task<IReadOnlyList<HistoryEntry>> LoadAsync(DateOnly from, DateOnly to, CancellationToken ct)
        => (await _history.ListRangeAsync(_currentUser.UserId, from, to, ct))
            .Where(t => t.CurrencyCode == ProjectionInputLoader.ProjectionCurrency.Code)
            .Select(t => new HistoryEntry(t.Date, t.Description, t.Amount, t.Category))
            .ToList();
}
