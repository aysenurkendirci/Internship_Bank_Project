using Bank.Domain.Common;
using Bank.Domain.Scheduling;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.CashFlows;

/// <summary>
/// A repeating income, expense or subscription such as salary, rent or a streaming plan.
/// </summary>
/// <remarks>
/// <see cref="Amount"/> is always positive and <see cref="Kind"/> carries the direction, matching
/// how transactions are stored. An outflow charged to a credit card does not leave the account on
/// its own date: it is paid with the card statement, which the cash-flow projection accounts for
/// through <see cref="CreditCardId"/>.
/// </remarks>
public sealed class RecurringCashFlow
{
    public long Id { get; }
    public string Name { get; }
    public CashFlowKind Kind { get; }
    public string Category { get; }
    public Money Amount { get; }
    public RecurrenceRule Recurrence { get; }
    public long? CreditCardId { get; }

    public RecurringCashFlow(
        long id,
        string name,
        CashFlowKind kind,
        string category,
        Money amount,
        RecurrenceRule recurrence,
        long? creditCardId = null)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentNullException.ThrowIfNull(recurrence);

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Kalem adı boş olamaz.");
        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("Kategori boş olamaz.");
        if (!Enum.IsDefined(kind))
            throw new DomainException($"Geçersiz kalem türü: {kind}.");
        if (!amount.IsPositive)
            throw new DomainException("Tutar sıfırdan büyük olmalıdır.");
        if (kind == CashFlowKind.Income && creditCardId is not null)
            throw new DomainException("Gelir bir kredi kartına bağlanamaz.");

        Id = id;
        Name = name.Trim();
        Kind = kind;
        Category = category.Trim();
        Amount = amount;
        Recurrence = recurrence;
        CreditCardId = creditCardId;
    }

    public bool IsOutflow => Kind != CashFlowKind.Income;

    public bool IsChargedToCard => CreditCardId is not null;

    public IEnumerable<CashFlowOccurrence> OccurrencesBetween(DateOnly from, DateOnly to)
    {
        var netAmount = IsOutflow ? -Amount : Amount;

        return Recurrence
            .OccurrencesBetween(from, to)
            .Select(date => new CashFlowOccurrence(date, Name, Kind, netAmount));
    }
}
