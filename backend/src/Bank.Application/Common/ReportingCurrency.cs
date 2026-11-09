using Bank.Domain.ValueObjects;

namespace Bank.Application.Common;

/// <summary>
/// The one currency projections and analytics are calculated in. Mixing currencies would need exchange rates
/// the system does not have, so amounts in anything else are left out of those figures.
/// </summary>
public static class ReportingCurrency
{
    public static readonly Currency Value = Currency.TRY;
}
