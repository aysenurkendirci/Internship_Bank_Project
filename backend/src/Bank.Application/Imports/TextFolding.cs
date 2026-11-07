using System.Text;

namespace Bank.Application.Imports;

/// <summary>
/// Reduces text to a form where spelling variants of the same word compare equal:
/// "MİGROS", "Migros" and "MIGROS" all become "migros".
/// </summary>
/// <remarks>
/// Culture-aware casing alone cannot do this. Invariant casing leaves the Turkish "İ" and "ı"
/// unchanged ("TARİH" → "tarİh", "Kadıköy" → "KADıKÖY"), while tr-TR casing turns the "I" in
/// "DESCRIPTION" into a dotless "ı". Turkish letters are therefore folded to ASCII explicitly.
/// </remarks>
public static class TextFolding
{
    public static string Fold(string text)
    {
        // NFC first, so a decomposed "I + combining dot" becomes a single "İ" handled below.
        var lower = text.Normalize(NormalizationForm.FormC).ToLowerInvariant();

        var builder = new StringBuilder(lower.Length);
        foreach (var word in lower.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (builder.Length > 0)
                builder.Append(' ');

            foreach (var c in word)
                builder.Append(c switch
                {
                    'ı' or 'İ' => 'i',
                    'ş' => 's',
                    'ğ' => 'g',
                    'ü' => 'u',
                    'ö' => 'o',
                    'ç' => 'c',
                    _ => c
                });
        }

        return builder.ToString();
    }
}
