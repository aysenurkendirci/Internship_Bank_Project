using Bank.Domain.Exceptions;

namespace Bank.Domain.ValueObjects;

/// <summary>
/// DDD Value Object: TC Kimlik Numarası
/// Türkiye Cumhuriyeti TC No algoritmik kontrolünü gerçekleştiren güvenli tip.
/// </summary>
public sealed record TcNo
{
    public string Value { get; }

    private TcNo(string value)
    {
        if (!IsValid(value))
            throw new InvalidTcNoException(value);

        Value = value;
    }

    public static TcNo Create(string value) => new(value);

    public static bool IsValid(string tcNo)
    {
        if (string.IsNullOrWhiteSpace(tcNo) || tcNo.Length != 11 || !tcNo.All(char.IsDigit))
            return false;

        if (tcNo[0] == '0')
            return false;

        int[] digits = tcNo.Select(digitChar => digitChar - '0').ToArray();

        // 1, 3, 5, 7, 9. hanelerin toplamının 7 katından, 2, 4, 6, 8. hanelerin toplamı çıkarılırsa,
        // elde edilen sonucun 10'a bölümünden kalan (mod 10) 10. haneyi verir.
        int oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        int evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        int tenthDigit = ((oddSum * 7) - evenSum) % 10;
        if (tenthDigit < 0) tenthDigit += 10;

        if (digits[9] != tenthDigit)
            return false;

        // İlk 10 hanenin toplamının 10'a bölümünden kalan (mod 10) 11. haneyi verir.
        int sumFirstTen = digits.Take(10).Sum();
        int eleventhDigit = sumFirstTen % 10;

        return digits[10] == eleventhDigit;
    }

    public override string ToString() => Value;
}
