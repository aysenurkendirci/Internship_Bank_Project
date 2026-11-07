using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common;
using Bank.Domain.Projection;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Features.Planning;

/// <summary>Gathers the current user's finance data into the input the projection engine expects.</summary>
public sealed class ProjectionInputLoader
{
    public static readonly Currency ProjectionCurrency = Currency.TRY;

    private readonly ICreditCardRepository _cards;
    private readonly IRecurringCashFlowRepository _cashFlows;
    private readonly IInstallmentPlanRepository _plans;
    private readonly IAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public ProjectionInputLoader(
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

    public async Task<ProjectionInput> LoadAsync(int months, CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        var cards = await _cards.ListAsync(userId, ct);
        var cashFlows = await _cashFlows.ListAsync(userId, kind: null, ct);
        var plans = await _plans.ListAsync(userId, ct);
        var balance = await _accounts.GetAvailableBalanceAsync(userId, ProjectionCurrency.Code, ct);

        // Today's movements are assumed to be in the current balance already (ADR-0003 §8).
        var from = BusinessDate.Today(_time).AddDays(1);

        return new ProjectionInput(
            from,
            months,
            new Money(balance, ProjectionCurrency),
            cashFlows.Select(c => c.Value).ToList(),
            cards.Select(c => c.Value).ToList(),
            plans);
    }
}
