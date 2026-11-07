using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Application.Common;
using Bank.Contracts.Finance;
using Bank.Domain.Projection;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Services.Finance;

/// <summary>
/// Loads the user's finance data and hands it to <see cref="CashFlowProjector"/>. All projection
/// rules live in the domain; this class only gathers inputs and maps the result.
/// </summary>
public sealed class ProjectionService : IProjectionService
{
    private static readonly Currency ProjectionCurrency = Currency.TRY;

    private readonly ICreditCardRepository _cards;
    private readonly IRecurringCashFlowRepository _cashFlows;
    private readonly IInstallmentPlanRepository _plans;
    private readonly IAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public ProjectionService(
        ICreditCardRepository cards,
        IRecurringCashFlowRepository cashFlows,
        IInstallmentPlanRepository plans,
        IAccountRepository accounts,
        ICurrentUser currentUser,
        TimeProvider time)
    {
        _cards = cards;
        _cashFlows = cashFlows;
        _plans = plans;
        _accounts = accounts;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ProjectionResponse> ProjectAsync(int months, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var cards = await _cards.ListAsync(userId, ct);
        var cashFlows = await _cashFlows.ListAsync(userId, kind: null, ct);
        var plans = await _plans.ListAsync(userId, ct);
        var balance = await _accounts.GetAvailableBalanceAsync(userId, ProjectionCurrency.Code, ct);

        // Today's movements are assumed to be in the current balance already (ADR-0003 §8).
        var from = BusinessDate.Today(_time).AddDays(1);

        var projection = CashFlowProjector.Project(new ProjectionInput(
            from,
            months,
            new Money(balance, ProjectionCurrency),
            cashFlows.Select(c => c.Value).ToList(),
            cards.Select(c => c.Value).ToList(),
            plans));

        return ToResponse(projection);
    }

    private static ProjectionResponse ToResponse(CashFlowProjection projection)
        => new(
            projection.From,
            projection.To,
            projection.OpeningBalance.Currency.Code,
            projection.OpeningBalance.Amount,
            projection.Months.Select(m => new ProjectionMonthResponse(
                m.Year,
                m.Month,
                m.Income.Amount,
                m.Outflows.Amount,
                m.CardPayments.Amount,
                m.Net.Amount,
                m.ClosingBalance.Amount,
                m.LowestBalance.Amount,
                m.LowestBalanceDate,
                m.BurdenRatio,
                m.IsDeficit,
                m.BalanceGoesNegative)).ToList(),
            projection.Entries.Select(e => new ProjectionEntryResponse(
                e.Date,
                e.Description,
                e.Kind.ToString(),
                e.Amount.Amount,
                e.CreditCardId,
                e.StatementDate,
                e.Charges.Select(c => new CardChargeResponse(c.Description, c.Amount.Amount)).ToList())).ToList(),
            projection.ExcludedItems);
}
