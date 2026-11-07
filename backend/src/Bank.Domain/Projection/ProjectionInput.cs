using Bank.Domain.Cards;
using Bank.Domain.CashFlows;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Projection;

/// <param name="From">First projected day (inclusive).</param>
/// <param name="Months">Length of the horizon in calendar months.</param>
/// <param name="OpeningBalance">Balance before <paramref name="From"/>; its currency is the projection currency.</param>
public sealed record ProjectionInput(
    DateOnly From,
    int Months,
    Money OpeningBalance,
    IReadOnlyList<RecurringCashFlow> CashFlows,
    IReadOnlyList<CreditCard> Cards,
    IReadOnlyList<InstallmentPlan> InstallmentPlans);
