using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Features.CreditCards;
using Bank.Application.Features.InstallmentPlans;
using Bank.Application.Features.RecurringCashFlows;
using Bank.Contracts.Account;
using Bank.Contracts.Finance;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Features.Privacy;

public sealed record ExportPersonalDataQuery : IRequest<PersonalDataExport>;

/// <summary>
/// Gathers every record held about the current user (KVKK article 11, right of access). Uses the same
/// mappings as the API endpoints, so the export holds exactly the shapes the user sees on screen.
/// </summary>
public sealed class ExportPersonalDataQueryHandler : IRequestHandler<ExportPersonalDataQuery, PersonalDataExport>
{
    // DATE parameters travel as SQL datetime, whose range starts in 1753; 1900 is safely before any real record.
    private static readonly DateOnly EarliestRecord = new(1900, 1, 1);
    private static readonly DateOnly LatestRecord = new(9999, 12, 31);

    private readonly IAuthRepository _users;
    private readonly ICreditCardRepository _cards;
    private readonly IRecurringCashFlowRepository _recurring;
    private readonly IInstallmentPlanRepository _plans;
    private readonly ITransactionHistoryRepository _history;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;
    private readonly ILogger<ExportPersonalDataQueryHandler> _logger;

    public ExportPersonalDataQueryHandler(
        IAuthRepository users,
        ICreditCardRepository cards,
        IRecurringCashFlowRepository recurring,
        IInstallmentPlanRepository plans,
        ITransactionHistoryRepository history,
        ICurrentUser currentUser,
        TimeProvider time,
        ILogger<ExportPersonalDataQueryHandler> logger)
    {
        _users = users;
        _cards = cards;
        _recurring = recurring;
        _plans = plans;
        _history = history;
        _currentUser = currentUser;
        _time = time;
        _logger = logger;
    }

    public async Task<PersonalDataExport> Handle(ExportPersonalDataQuery query, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var profile = await _users.GetProfileAsync(userId, ct);
        var cards = await _cards.ListAsync(userId, ct);
        var recurring = await _recurring.ListAsync(userId, kind: null, ct);
        var plans = await InstallmentPlanMapping.ListAsync(userId, _plans, _cards, ct);
        var transactions = await _history.ListRangeAsync(userId, EarliestRecord, LatestRecord, ct);

        // Exports leave the system, so each one is recorded.
        _logger.LogInformation("User {UserId} exported their personal data", userId);

        return new PersonalDataExport(
            _time.GetUtcNow().UtcDateTime,
            new PersonalProfile(
                profile.TcNo, profile.FirstName, profile.LastName, profile.Email, profile.Phone,
                profile.Membership, profile.CreatedAt, profile.LastLoginAt),
            cards.Select(CreditCardMapping.ToResponse).ToList(),
            recurring.Select(RecurringCashFlowMapping.ToResponse).ToList(),
            plans,
            transactions
                .Select(t => new HistoryTransactionResponse(t.Id, t.Date, t.Description, t.Amount, t.CurrencyCode, t.Category))
                .ToList());
    }
}
