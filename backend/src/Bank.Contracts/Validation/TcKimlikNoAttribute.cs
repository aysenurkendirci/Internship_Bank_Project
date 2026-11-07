using System.ComponentModel.DataAnnotations;

namespace Bank.Contracts.Validation;

/// <summary>
/// Validates a Turkish national identity number (TCKN): 11 digits, no leading zero,
/// and the two trailing check digits computed from the first nine.
/// A format-only check would accept numbers that cannot exist.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field)]
public sealed class TcKimlikNoAttribute : ValidationAttribute
{
    public TcKimlikNoAttribute() : base("Geçerli bir TC kimlik numarası giriniz.") { }

    // Null is left to [Required] so that the two concerns produce separate messages.
    public override bool IsValid(object? value)
        => value is null || (value is string tcNo && IsValidTcKimlikNo(tcNo));

    public static bool IsValidTcKimlikNo(string tcNo)
    {
        if (tcNo.Length != 11 || tcNo[0] == '0' || !tcNo.All(char.IsAsciiDigit))
            return false;

        var d = tcNo.Select(c => c - '0').ToArray();

        var oddSum = d[0] + d[2] + d[4] + d[6] + d[8];
        var evenSum = d[1] + d[3] + d[5] + d[7];
        var tenth = ((oddSum * 7 - evenSum) % 10 + 10) % 10;
        var eleventh = d.Take(10).Sum() % 10;

        return d[9] == tenth && d[10] == eleventh;
    }
}
