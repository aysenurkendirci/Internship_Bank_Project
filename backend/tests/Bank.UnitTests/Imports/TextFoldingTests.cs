using Bank.Application.Imports;

namespace Bank.UnitTests.Imports;

public sealed class TextFoldingTests
{
    [Theory]
    [InlineData("MİGROS", "migros")]
    [InlineData("MIGROS", "migros")]
    [InlineData("Migros", "migros")]
    [InlineData("TARİH", "tarih")]
    [InlineData("DESCRIPTION", "description")]
    [InlineData("Kadıköy", "kadikoy")]
    [InlineData("KADIKÖY", "kadikoy")]
    [InlineData("ŞOK ÇAĞLAYAN", "sok caglayan")]
    [InlineData("  İşlem   Tarihi ", "islem tarihi")]
    [InlineData("I\u0307STANBUL", "istanbul")]   // decomposed İ (I + combining dot) is composed first
    public void SpellingVariants_FoldToTheSameText(string input, string expected)
    {
        Assert.Equal(expected, TextFolding.Fold(input));
    }
}
