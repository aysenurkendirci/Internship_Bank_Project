using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Contracts.Account;
using Bank.Contracts.Finance;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Services;

public interface IPersonalDataService
{
    Task<PersonalDataExport> ExportAsync(CancellationToken ct = default);
}

/// <summary>
/// Gathers every record held about the current user (KVKK article 11, right of access). Reuses the finance
/// services so the export uses exactly the shapes the API already returns.
/// </summary>
public sealed class PersonalDataService : IPersonalDataService
{
    // DATE parameters travel as SQL datetime, whose range starts in 1753; 1900 is safely before any real record.
    private static readonly DateOnly EarliestRecord = new(1900, 1, 1);
    private static readonly DateOnly LatestRecord = new(9999, 12, 31);

    private readonly IAuthRepository _users;
    private readonly ITransactionHistoryRepository _history;
    private readonly ICreditCardService _cards;
    private readonly IRecurringCashFlowService _recurring;
    private readonly IInstallmentPlanService _plans;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;
    private readonly ILogger<PersonalDataService> _logger;

    public PersonalDataService(
        IAuthRepository users,
        ITransactionHistoryRepository history,
        ICreditCardService cards,
        IRecurringCashFlowService recurring,
        IInstallmentPlanService plans,
        ICurrentUser currentUser,
        TimeProvider time,
        ILogger<PersonalDataService> logger)
    {
        _users = users;
        _history = history;
        _cards = cards;
        _recurring = recurring;
        _plans = plans;
        _currentUser = currentUser;
        _time = time;
        _logger = logger;
    }

    public async Task<PersonalDataExport> ExportAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var profile = await _users.GetProfileAsync(userId, ct);
        var transactions = await _history.ListRangeAsync(userId, EarliestRecord, LatestRecord, ct);

        // Exports leave the system, so each one is recorded.
        _logger.LogInformation("User {UserId} exported their personal data", userId);

        return new PersonalDataExport(
            _time.GetUtcNow().UtcDateTime,
            new PersonalProfile(
                profile.TcNo, profile.FirstName, profile.LastName, profile.Email, profile.Phone,
                profile.Membership, profile.CreatedAt, profile.LastLoginAt),
            await _cards.ListAsync(ct),
            await _recurring.ListAsync(kind: null, ct),
            await _plans.ListAsync(ct),
            transactions
                .Select(t => new HistoryTransactionResponse(t.Id, t.Date, t.Description, t.Amount, t.CurrencyCode, t.Category))
                .ToList());
    }
}
