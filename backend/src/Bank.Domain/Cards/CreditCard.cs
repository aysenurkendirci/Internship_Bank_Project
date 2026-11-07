using Bank.Domain.Common;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Cards;

/// <summary>
/// A credit card's billing cycle. Spending on a card does not leave the bank account on the day
/// of purchase: it is collected on a statement and paid on that statement's due date.
/// </summary>
/// <remarks>
/// Rules modelled here:
/// <list type="bullet">
///   <item>A purchase made on or before the statement day belongs to that month's statement, later ones to the next.</item>
///   <item>A statement day beyond the month's length closes on the last day (31 → 28 Feb) without drifting.</item>
///   <item>The due date is <see cref="PaymentDueDays"/> after the statement; if it falls on a weekend it moves to Monday.</item>
/// </list>
/// Public holidays are not modelled; a due date on a holiday is treated as a business day.
/// </remarks>
public sealed class CreditCard
{
    public long Id { get; }
    public string Name { get; }
    public Money Limit { get; }
    public int StatementDay { get; }
    public int PaymentDueDays { get; }

    public CreditCard(long id, string name, Money limit, int statementDay, int paymentDueDays)
    {
        ArgumentNullException.ThrowIfNull(limit);

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Kart adı boş olamaz.");
        if (!limit.IsPositive)
            throw new DomainException("Kart limiti sıfırdan büyük olmalıdır.");
        if (statementDay is < 1 or > 31)
            throw new DomainException("Hesap kesim günü 1 ile 31 arasında olmalıdır.");
        if (paymentDueDays is < 1 or > 30)
            throw new DomainException("Son ödeme süresi 1 ile 30 gün arasında olmalıdır.");

        Id = id;
        Name = name.Trim();
        Limit = limit;
        StatementDay = statementDay;
        PaymentDueDays = paymentDueDays;
    }

    /// <summary>The statement a purchase made on <paramref name="transactionDate"/> appears on.</summary>
    public DateOnly StatementDateFor(DateOnly transactionDate)
    {
        var statementThisMonth = StatementDateIn(transactionDate.Year, transactionDate.Month);

        return transactionDate <= statementThisMonth
            ? statementThisMonth
            : StatementDateIn(transactionDate.AddMonths(1));
    }

    public DateOnly NextStatementDate(DateOnly statementDate)
        => StatementDateIn(statementDate.AddMonths(1));

    public DateOnly DueDateFor(DateOnly statementDate)
        => NextBusinessDay(statementDate.AddDays(PaymentDueDays));

    private DateOnly StatementDateIn(DateOnly anyDayInMonth)
        => StatementDateIn(anyDayInMonth.Year, anyDayInMonth.Month);

    // Derived from StatementDay every time, so a short month never shifts later statements.
    private DateOnly StatementDateIn(int year, int month)
        => new(year, month, Math.Min(StatementDay, DateTime.DaysInMonth(year, month)));

    private static DateOnly NextBusinessDay(DateOnly date) => date.DayOfWeek switch
    {
        DayOfWeek.Saturday => date.AddDays(2),
        DayOfWeek.Sunday => date.AddDays(1),
        _ => date
    };
}
