using Bank.Contracts.Finance;
using Bank.Domain.Analytics;
using Bank.Domain.Common;
using FluentValidation;
using MediatR;

namespace Bank.Application.Features.Analytics;

/// <summary>Expected everyday spending with a 95% interval; needs at least six months of history.</summary>
public sealed record GetSpendingForecastQuery(int Months) : IRequest<SpendingForecastResponse>;

public sealed class GetSpendingForecastQueryValidator : AbstractValidator<GetSpendingForecastQuery>
{
    public GetSpendingForecastQueryValidator()
    {
        RuleFor(q => q.Months).InclusiveBetween(1, 12).WithMessage("Süre 1 ile 12 ay arasında olmalıdır.");
    }
}

public sealed class GetSpendingForecastQueryHandler : IRequestHandler<GetSpendingForecastQuery, SpendingForecastResponse>
{
    private const int MaxHistoryMonths = 24;

    private readonly AnalyticsHistory _history;

    public GetSpendingForecastQueryHandler(AnalyticsHistory history) => _history = history;

    public async Task<SpendingForecastResponse> Handle(GetSpendingForecastQuery query, CancellationToken ct)
    {
        var last = _history.LastCompleteMonth;
        var entries = await _history.LoadAsync(last.AddMonths(-(MaxHistoryMonths - 1)).FirstDay, last.LastDay, ct);

        // Start at the first month with any data, so a short history is not padded with empty months.
        var first = entries.Count == 0 ? last : YearMonth.Of(entries.Min(e => e.Date));
        var count = (last.Year - first.Year) * 12 + last.Month - first.Month + 1;
        var series = EverydaySpending.MonthlyTotals(entries, first, count);

        var forecast = SpendingForecaster.Forecast(series, last, query.Months);

        return new SpendingForecastResponse(
            forecast.Method.ToString(),
            series.Count,
            forecast.Months.Select(m => new ForecastMonthResponse(m.Month.ToString(), m.Expected, m.Lower, m.Upper)).ToList(),
            forecast.Backtests.Select(b => new ForecastBacktestResponse(
                b.Method.ToString(), b.Forecasts, Math.Round(b.MeanAbsolutePercentageError, 4), Math.Round(b.IntervalCoverage, 4))).ToList());
    }
}
