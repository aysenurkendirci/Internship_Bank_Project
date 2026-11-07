using Bank.Contracts.Finance;
using Bank.Domain.Analytics;
using Bank.Domain.Common;
using FluentValidation;
using MediatR;

namespace Bank.Application.Features.Analytics;

/// <summary>Unusually large purchases in the last months, each explained against a typical amount.</summary>
public sealed record GetAnomaliesQuery(int Months) : IRequest<IReadOnlyList<AnomalyResponse>>;

public sealed class GetAnomaliesQueryValidator : AbstractValidator<GetAnomaliesQuery>
{
    public GetAnomaliesQueryValidator()
    {
        RuleFor(q => q.Months).InclusiveBetween(1, 12).WithMessage("Süre 1 ile 12 ay arasında olmalıdır.");
    }
}

public sealed class GetAnomaliesQueryHandler : IRequestHandler<GetAnomaliesQuery, IReadOnlyList<AnomalyResponse>>
{
    private readonly AnalyticsHistory _history;

    public GetAnomaliesQueryHandler(AnalyticsHistory history) => _history = history;

    public async Task<IReadOnlyList<AnomalyResponse>> Handle(GetAnomaliesQuery query, CancellationToken ct)
    {
        // The baseline covers a full year even when only recent months are reported,
        // so "unusual" is judged against the user's habits rather than a few weeks.
        var entries = await _history.LoadBaselineAsync(ct);
        var since = YearMonth.Of(_history.Today).AddMonths(-(query.Months - 1)).FirstDay;

        return AnomalyDetector.Detect(entries)
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
}
