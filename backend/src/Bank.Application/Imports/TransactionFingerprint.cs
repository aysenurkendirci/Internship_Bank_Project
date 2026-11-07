using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bank.Domain.Common;

namespace Bank.Application.Imports;

/// <summary>
/// Identifies a statement line so the same line is never stored twice, even when the same file
/// is uploaded again or two statements overlap.
/// </summary>
/// <remarks>
/// Date, amount and description alone are not enough: two identical coffees on the same day are
/// two real transactions. The fingerprint therefore also includes the line's position among
/// identical lines in its file (1st, 2nd, ...). Re-importing the file, or another statement
/// covering the same day, yields the same positions and therefore the same fingerprints.
/// </remarks>
public static class TransactionFingerprint
{
    public static IReadOnlyList<byte[]> For(IReadOnlyList<StatementLine> lines)
    {
        var seen = new Dictionary<string, int>();
        var fingerprints = new byte[lines.Count][];

        for (var i = 0; i < lines.Count; i++)
        {
            var key = Key(lines[i]);
            var occurrence = seen[key] = seen.GetValueOrDefault(key) + 1;
            fingerprints[i] = SHA256.HashData(Encoding.UTF8.GetBytes($"{key}|{occurrence}"));
        }

        return fingerprints;
    }

    // Casing, spacing and Turkish spelling variants ("MİGROS" / "MIGROS") must not create a "new" line.
    private static string Key(StatementLine line)
        => string.Join('|',
            line.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            line.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            TextFolding.Fold(line.Description));
}
