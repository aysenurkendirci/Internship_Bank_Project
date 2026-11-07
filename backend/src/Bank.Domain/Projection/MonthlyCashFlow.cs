using Bank.Domain.ValueObjects;

namespace Bank.Domain.Projection;

/// <summary>
/// One calendar month of the projection. Amounts are positive totals except <see cref="Net"/> and the
/// balances, which can be negative.
/// </summary>
/// <param name="Outflows">Every outflow of the month, card payments included.</param>
/// <param name="LowestBalance">
/// The lowest running balance within the month. A month can close positive while dipping below
/// zero before the salary arrives; this is the figure that exposes that.
/// </param>
/// <param name="BurdenRatio">Outflows divided by income; <c>null</c> when the month has no income.</param>
public sealed record MonthlyCashFlow(
    int Year,
    int Month,
    Money Income,
    Money Outflows,
    Money CardPayments,
    Money Net,
    Money ClosingBalance,
    Money LowestBalance,
    DateOnly? LowestBalanceDate,
    decimal? BurdenRatio)
{
    public bool IsDeficit => Net.IsNegative;
    public bool BalanceGoesNegative => LowestBalance.IsNegative;
}
