using Bank.Domain.Common;

namespace Bank.Domain.Accounts;

/// <summary>
/// A Turkish IBAN: "TR", two check digits, a five-digit bank code, a reserve digit ("0") and a sixteen-digit
/// account number, 26 characters in all.
/// </summary>
/// <remarks>
/// Validated with the ISO 13616 mod-97 check, which catches every single mistyped digit and almost every pair of
/// swapped digits. A transfer to a mistyped IBAN is therefore rejected before any account is even looked up.
/// </remarks>
public sealed record Iban
{
    public const int Length = 26;
    private const string CountryCode = "TR";

    public string Value { get; }

    private Iban(string value) => Value = value;

    public static Iban Parse(string? text)
        => TryParse(text, out var iban) ? iban : throw new DomainException("Geçersiz IBAN.");

    /// <summary>Accepts the printed form too: spaces are ignored and letters may be lower case.</summary>
    public static bool TryParse(string? text, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Iban? iban)
    {
        iban = null;
        if (text is null)
            return false;

        var compact = string.Concat(text.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
        if (compact.Length != Length || !compact.StartsWith(CountryCode, StringComparison.Ordinal))
            return false;
        if (!compact.Skip(2).All(char.IsAsciiDigit))
            return false;
        if (Mod97(compact[4..] + compact[..4]) != 1)
            return false;

        iban = new Iban(compact);
        return true;
    }

    /// <summary>Builds the IBAN of an account held at the bank with the given five-digit code.</summary>
    public static Iban For(string bankCode, long accountNumber)
    {
        if (bankCode.Length != 5 || !bankCode.All(char.IsAsciiDigit))
            throw new ArgumentException("Bank code must be five digits.", nameof(bankCode));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(accountNumber);

        var bban = $"{bankCode}0{accountNumber:D16}";
        var checkDigits = 98 - Mod97(bban + CountryCode + "00");
        return new Iban($"{CountryCode}{checkDigits:D2}{bban}");
    }

    /// <summary>The usual printed form in groups of four: "TR12 0006 1005 ...".</summary>
    public string Formatted => string.Join(' ', Value.Chunk(4).Select(chunk => new string(chunk)));

    public override string ToString() => Value;

    // Letters count as two digits (A = 10 ... Z = 35); the remainder is carried so no big integers are needed.
    private static int Mod97(string text)
    {
        var remainder = 0;
        foreach (var c in text)
            remainder = char.IsAsciiDigit(c)
                ? (remainder * 10 + (c - '0')) % 97
                : (remainder * 100 + (c - 'A' + 10)) % 97;
        return remainder;
    }
}
