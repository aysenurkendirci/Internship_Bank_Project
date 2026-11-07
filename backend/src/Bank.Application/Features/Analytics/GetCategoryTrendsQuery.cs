using Bank.Contracts.Finance;
using Bank.Domain.Analytics;
using FluentValidation;
using MediatR;

namespace Bank.Application.Features.Analytics;

/// <summary>Spending per category over the last complete months.</summary>
public sealed record GetCategoryTrendsQuery(int Months) : IRequest<CategoryTrendsResponse>;

public sealed class GetCategoryTrendsQueryValidator : AbstractValidator<GetCategoryTrendsQuery>
{
    public GetCategoryTrendsQueryValidator()
    {
        RuleFor(q => q.Months).InclusiveBetween(1, 24).WithMessage("Süre 1 ile 24 ay arasında olmalıdır.");
    }
}

public sealed class GetCategoryTrendsQueryHandler : IRequestHandler<GetCategoryTrendsQuery, CategoryTrendsResponse>
{
    private readonly AnalyticsHistory _history;

    public GetCategoryTrendsQueryHandler(AnalyticsHistory history) => _history = history;

    public async Task<CategoryTrendsResponse> Handle(GetCategoryTrendsQuery query, CancellationToken ct)
    {
        var last = _history.LastCompleteMonth;
        var first = last.AddMonths(-(query.Months - 1));
        var entries = await _history.LoadAsync(first.FirstDay, last.LastDay, ct);

        var report = CategoryTrendAnalyzer.Analyze(entries, first, query.Months);

        return new CategoryTrendsResponse(
            report.Months.Select(m => m.ToString()).ToList(),
            report.MonthlyTotals,
            report.Categories.Select(c => new CategoryTrendResponse(
                c.Category, c.MonthlySpending, c.Total, c.AverageMonthly, c.Share, c.ChangeFromPreviousMonth, c.SlopePerMonth)).ToList());
    }
}
