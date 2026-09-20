using Bank.Domain.Exceptions;

namespace Bank.Domain.ValueObjects;

/// <summary>
/// DDD Value Object: IBAN (International Bank Account Number)
/// Türkiye formatındaki IBAN'ları doğrular ve standartlaştırır.
/// </summary>
public sealed record Iban
{
    public string Value { get; }

    private Iban(string value)
    {
        var cleaned = value.Replace(" ", "").ToUpperInvariant();

        if (!IsValid(cleaned))
            throw new InvalidIbanException(value);

        Value = cleaned;
    }

    public static Iban Create(string value) => new(value);

    public static bool IsValid(string iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return false;

        var clean = iban.Replace(" ", "").ToUpperInvariant();

        // Türkiye IBAN'ları TR ile başlar ve tam 26 karakterdir.
        if (!clean.StartsWith("TR") || clean.Length != 26)
            return false;

        // Karakterlerin alfanümerik kontrolü
        if (!clean.All(char.IsLetterOrDigit))
            return false;

        return true;
    }

    public string Formatted => string.Join(" ", Enumerable.Range(0, Value.Length / 4 + 1)
        .Select(index => Value.Substring(index * 4, Math.Min(4, Value.Length - index * 4)))
        .Where(segment => segment.Length > 0));

    public override string ToString() => Value;
}
