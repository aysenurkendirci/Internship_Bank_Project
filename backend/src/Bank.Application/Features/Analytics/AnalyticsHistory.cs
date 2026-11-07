using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common;
using Bank.Application.Features.Planning;
using Bank.Domain.Analytics;
using Bank.Domain.Common;

namespace Bank.Application.Features.Analytics;

/// <summary>
/// Loads the current user's transaction history in the shape the domain analytics expect. Trends and
/// forecasts use complete months only: comparing a half-finished month with a full one would report a
/// false drop.
/// </summary>
public sealed class AnalyticsHistory
{
    // Long enough for the detectors to learn the user's habits, short enough to reflect current ones.
    private const int BaselineMonths = 12;

    private readonly ITransactionHistoryRepository _history;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public AnalyticsHistory(ITransactionHistoryRepository history, ICurrentUser currentUser, TimeProvider time)
    {
        _history = history;
        _currentUser = currentUser;
        _time = time;
    }

    public DateOnly Today => BusinessDate.Today(_time);

    public YearMonth LastCompleteMonth => YearMonth.Of(Today).AddMonths(-1);

    /// <summary>The last twelve months, used where "unusual" or "recurring" is judged against habits.</summary>
    public Task<IReadOnlyList<HistoryEntry>> LoadBaselineAsync(CancellationToken ct)
        => LoadAsync(Today.AddMonths(-BaselineMonths), Today, ct);

    public async Task<IReadOnlyList<HistoryEntry>> LoadAsync(DateOnly from, DateOnly to, CancellationToken ct)
        => (await _history.ListRangeAsync(_currentUser.UserId, from, to, ct))
            .Where(t => t.CurrencyCode == ProjectionInputLoader.ProjectionCurrency.Code)
            .Select(t => new HistoryEntry(t.Date, t.Description, t.Amount, t.Category))
            .ToList();
}
