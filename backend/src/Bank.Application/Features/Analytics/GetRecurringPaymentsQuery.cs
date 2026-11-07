using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Contracts.Finance;
using Bank.Domain.Analytics;
using Bank.Domain.Common;
using MediatR;

namespace Bank.Application.Features.Analytics;

/// <summary>Payments that repeat monthly; untracked ones are candidates to add as recurring items.</summary>
public sealed record GetRecurringPaymentsQuery : IRequest<IReadOnlyList<RecurringPaymentResponse>>;

public sealed class GetRecurringPaymentsQueryHandler
    : IRequestHandler<GetRecurringPaymentsQuery, IReadOnlyList<RecurringPaymentResponse>>
{
    private readonly AnalyticsHistory _history;
    private readonly IRecurringCashFlowRepository _recurringItems;
    private readonly ICurrentUser _currentUser;

    public GetRecurringPaymentsQueryHandler(
        AnalyticsHistory history, IRecurringCashFlowRepository recurringItems, ICurrentUser currentUser)
    {
        _history = history;
        _recurringItems = recurringItems;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<RecurringPaymentResponse>> Handle(GetRecurringPaymentsQuery query, CancellationToken ct)
    {
        var entries = await _history.LoadBaselineAsync(ct);
        var tracked = (await _recurringItems.ListAsync(_currentUser.UserId, kind: null, ct))
            .Select(i => TextFolding.Fold(i.Value.Name))
            .ToList();

        return RecurringPaymentDetector.Detect(entries)
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

    /// <summary>
    /// Names typed by the user rarely match statement text exactly ("Netflix" vs "NETFLIX.COM"), so a
    /// payment counts as tracked when either folded name contains the other.
    /// </summary>
    private static bool IsTracked(string merchant, IEnumerable<string> trackedNames)
    {
        var folded = TextFolding.Fold(merchant);
        return trackedNames.Any(name => name.Length > 0 && (folded.Contains(name) || name.Contains(folded)));
    }
}
