using System.Globalization;
using System.Text.RegularExpressions;

namespace Bank.Application.Imports;

/// <summary>
/// Parses amounts as banks export them: "1.234,56", "-1.234,56 TL", "1,234.56", "₺ 50".
/// </summary>
/// <remarks>
/// The decimal separator is whichever of '.' or ',' appears last and is followed by one or two
/// digits; every other separator is a thousands separator. So "1.234,5" is 1234.5 and "1.234" is
/// 1234 (three digits after the dot means thousands, as Turkish exports always print decimals).
/// Thousands groups must be exactly three digits and use the other separator; anything else
/// ("1.2.3", "12,3,4") is rejected rather than guessed at.
/// </remarks>
public static partial class StatementAmount
{
    private static readonly string[] CurrencyMarkers = ["TRY", "TL", "₺"];

    public static bool TryParse(string? raw, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var text = raw.Trim();
        foreach (var marker in CurrencyMarkers)
            text = text.Replace(marker, "", StringComparison.OrdinalIgnoreCase);

        // Spaces, including the non-breaking space Excel uses as a thousands separator.
        text = text.Replace(" ", "").Replace("\u00A0", "");

        var negative = text.StartsWith('-') || text.EndsWith('-');
        text = text.Trim('-', '+');

        var lastSeparator = text.LastIndexOfAny(['.', ',']);
        var hasDecimals = lastSeparator >= 0 && text.Length - lastSeparator - 1 is 1 or 2;

        var integerText = hasDecimals ? text[..lastSeparator] : text;
        var fractionPart = hasDecimals ? text[(lastSeparator + 1)..] : "";

        if (!IsWellGrouped(integerText, decimalSeparator: hasDecimals ? text[lastSeparator] : null))
            return false;

        var integerPart = integerText.Replace(".", "").Replace(",", "");

        var normalized = fractionPart.Length > 0 ? $"{integerPart}.{fractionPart}" : integerPart;
        if (!PlainNumber().IsMatch(normalized))
            return false;

        amount = decimal.Parse(normalized, CultureInfo.InvariantCulture);
        if (negative)
            amount = -amount;

        return true;
    }

    private static bool IsWellGrouped(string integerText, char? decimalSeparator)
    {
        if (!integerText.Contains('.') && !integerText.Contains(','))
            return true;

        // A thousands separator cannot be the same character as the decimal separator,
        // and only one kind of thousands separator may appear.
        var thousands = integerText.Contains('.') ? '.' : ',';
        if (thousands == decimalSeparator || integerText.Contains(thousands == '.' ? ',' : '.'))
            return false;

        return thousands == '.' ? DotGrouped().IsMatch(integerText) : CommaGrouped().IsMatch(integerText);
    }

    [GeneratedRegex(@"^\d+(\.\d{1,2})?$")]
    private static partial Regex PlainNumber();

    [GeneratedRegex(@"^\d{1,3}(\.\d{3})+$")]
    private static partial Regex DotGrouped();

    [GeneratedRegex(@"^\d{1,3}(,\d{3})+$")]
    private static partial Regex CommaGrouped();
}
