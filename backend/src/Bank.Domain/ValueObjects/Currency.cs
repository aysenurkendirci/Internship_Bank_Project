using Bank.Domain.Common;

namespace Bank.Domain.ValueObjects;

/// <summary>
/// ISO 4217 currency. Only currencies with two minor-unit digits are supported,
/// which lets <see cref="Money"/> round every amount to cents.
/// </summary>
public sealed record Currency
{
    public static readonly Currency TRY = new("TRY");
    public static readonly Currency USD = new("USD");
    public static readonly Currency EUR = new("EUR");

    private static readonly IReadOnlyDictionary<string, Currency> Supported =
        new[] { TRY, USD, EUR }.ToDictionary(c => c.Code);

    public string Code { get; }

    private Currency(string code) => Code = code;

    public static Currency FromCode(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return Supported.TryGetValue(code.ToUpperInvariant(), out var currency)
            ? currency
            : throw new DomainException($"Desteklenmeyen para birimi: '{code}'.");
    }

    public override string ToString() => Code;
}
