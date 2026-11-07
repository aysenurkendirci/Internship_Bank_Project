using Bank.Application.Imports;

namespace Bank.UnitTests.Imports;

public sealed class TransactionFingerprintTests
{
    private static StatementLine Line(string description, decimal amount = -50m, int day = 5)
        => new(1, new DateOnly(2026, 3, day), description, amount, null);

    [Fact]
    public void IdenticalLinesInOneFile_AreKeptApart()
    {
        // Two coffees on the same day for the same amount are two real purchases.
        var fingerprints = TransactionFingerprint.For([Line("KAHVE DÜNYASI"), Line("KAHVE DÜNYASI")]);

        Assert.NotEqual(fingerprints[0], fingerprints[1]);
    }

    [Fact]
    public void SameFileTwice_GivesTheSameFingerprints()
    {
        StatementLine[] file = [Line("KAHVE DÜNYASI"), Line("KAHVE DÜNYASI"), Line("MİGROS", -300m)];

        Assert.Equal(TransactionFingerprint.For(file), TransactionFingerprint.For(file));
    }

    [Fact]
    public void OverlappingStatement_RecognisesTheSharedDay()
    {
        // January–March and March–May statements both contain 5 March.
        var first = TransactionFingerprint.For([Line("KİRA", -20_000m, day: 1), Line("KAHVE", day: 5), Line("KAHVE", day: 5)]);
        var second = TransactionFingerprint.For([Line("KAHVE", day: 5), Line("KAHVE", day: 5), Line("MARKET", day: 9)]);

        Assert.Equal(first[1], second[0]);
        Assert.Equal(first[2], second[1]);
    }

    [Fact]
    public void CaseAndSpacingDifferences_DoNotCreateANewLine()
    {
        var a = TransactionFingerprint.For([Line("Migros  Kadıköy ")]);
        var b = TransactionFingerprint.For([Line("MIGROS KADIKÖY")]);

        Assert.Equal(a[0], b[0]);
    }

    [Fact]
    public void TurkishSpellingVariants_AreTheSameLine()
    {
        // One export writes "MİGROS", another "MIGROS"; an overlapping import must still match them.
        var a = TransactionFingerprint.For([Line("MİGROS KADIKÖY")]);
        var b = TransactionFingerprint.For([Line("MIGROS KADIKOY")]);

        Assert.Equal(a[0], b[0]);
    }

    [Fact]
    public void DifferentAmountOrDate_IsADifferentLine()
    {
        var fingerprints = TransactionFingerprint.For([Line("KAHVE", -50m), Line("KAHVE", -55m), Line("KAHVE", -50m, day: 6)]);

        Assert.Equal(3, fingerprints.Distinct(new ByteArrayComparer()).Count());
    }

    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y) => x.AsSpan().SequenceEqual(y);
        public int GetHashCode(byte[] obj) => BitConverter.ToInt32(obj, 0);
    }
}
