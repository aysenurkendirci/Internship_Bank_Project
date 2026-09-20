using Bank.Domain.Exceptions;

namespace Bank.Domain.ValueObjects;

/// <summary>
/// Domain-Driven Design (DDD) Value Object: Money (Para)
/// Bir değer nesnesidir (Value Object): Değiştirilemez (Immutable), 
/// İki Money nesnesi ancak Tutar ve Para Birimi eşitse eşittir.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Para birimi boş olamaz.");

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Create(decimal amount, string currency = "TRY")
    {
        return new Money(amount, currency);
    }

    public static Money Zero(string currency = "TRY") => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static bool operator >(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return left.Amount <= right.Amount;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new CurrencyMismatchException(Currency, other.Currency);
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
