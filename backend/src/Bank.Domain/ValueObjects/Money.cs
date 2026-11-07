using System.Globalization;
using Bank.Domain.Common;

namespace Bank.Domain.ValueObjects;

/// <summary>
/// An amount bound to its currency. Arithmetic and comparison between different
/// currencies is rejected instead of silently producing a meaningless number.
/// </summary>
/// <remarks>
/// Amounts keep full decimal precision during calculations; <see cref="Round"/> brings them
/// to cents. Midpoints round away from zero (2.345 → 2.35), the convention customers expect
/// on statements, rather than .NET's default banker's rounding (2.345 → 2.34).
/// </remarks>
public sealed record Money : IComparable<Money>
{
    private const int MinorUnitDigits = 2;

    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money(decimal amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(Currency currency) => new(0m, currency);

    public bool IsZero => Amount == 0m;
    public bool IsNegative => Amount < 0m;
    public bool IsPositive => Amount > 0m;

    public Money Round()
        => new(Math.Round(Amount, MinorUnitDigits, MidpointRounding.AwayFromZero), Currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator -(Money money) => new(-money.Amount, money.Currency);

    public static Money operator *(Money money, decimal factor) => new(money.Amount * factor, money.Currency);

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public int CompareTo(Money? other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    public override string ToString()
        => $"{Amount.ToString("0.00", CultureInfo.InvariantCulture)} {Currency}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new DomainException($"{left.Currency} ile {right.Currency} tutarları birlikte işlenemez.");
    }
}
