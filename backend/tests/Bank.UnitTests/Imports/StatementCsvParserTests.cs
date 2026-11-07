using System.Text;
using Bank.Application.Imports;

namespace Bank.UnitTests.Imports;

public sealed class StatementCsvParserTests
{
    static StatementCsvParserTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static StatementParseResult Parse(string csv, Encoding? encoding = null)
        => StatementCsvParser.Parse((encoding ?? new UTF8Encoding(false)).GetBytes(csv));

    [Fact]
    public void TurkishExport_WithSemicolonsAndTurkishNumbers()
    {
        var result = Parse("""
            Tarih;Açıklama;Tutar;Kategori
            05.03.2026;MİGROS KADIKÖY;-1.234,56;Market
            15.03.2026;MAAŞ ÖDEMESİ;45.000,00;Maaş
            """);

        Assert.True(result.IsValid);
        Assert.Equal(
            [
                new StatementLine(2, new(2026, 3, 5), "MİGROS KADIKÖY", -1234.56m, "Market"),
                new StatementLine(3, new(2026, 3, 15), "MAAŞ ÖDEMESİ", 45000m, "Maaş")
            ],
            result.Lines);
    }

    [Fact]
    public void Windows1254File_FromTurkishExcel_KeepsTurkishLetters()
    {
        var result = Parse("Tarih;Açıklama;Tutar\n05.03.2026;ŞOK MARKET ÇAĞLAYAN;-89,90\n", Encoding.GetEncoding(1254));

        Assert.Equal("ŞOK MARKET ÇAĞLAYAN", Assert.Single(result.Lines).Description);
    }

    [Fact]
    public void Utf8WithBom_IsRead()
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("Tarih;Açıklama;Tutar\n05.03.2026;Kira;-20.000,00\n")).ToArray();

        var result = StatementCsvParser.Parse(bytes);

        Assert.Equal(-20000m, Assert.Single(result.Lines).Amount);
    }

    [Fact]
    public void CommaSeparated_WithQuotedFieldsContainingDelimiters()
    {
        var result = Parse("""
            Date,Description,Amount
            2026-03-05,"Migros, Kadıköy","-1,234.56"
            """);

        var line = Assert.Single(result.Lines);
        Assert.Equal("Migros, Kadıköy", line.Description);
        Assert.Equal(-1234.56m, line.Amount);
    }

    [Fact]
    public void DebitAndCreditColumns_BecomeOneSignedAmount()
    {
        var result = Parse("""
            İşlem Tarihi;İşlem Açıklaması;Borç;Alacak
            05.03.2026;Elektrik faturası;450,75;
            15.03.2026;Maaş;;45.000,00
            """);

        Assert.Equal([-450.75m, 45000m], result.Lines.Select(l => l.Amount));
    }

    [Theory]
    [InlineData("TARİH;AÇIKLAMA;TUTAR")]       // invariant lower-casing would leave a combining dot after "i"
    [InlineData("DATE;DESCRIPTION;AMOUNT")]    // Turkish lower-casing would produce a dotless "ı"
    [InlineData("tarih;aciklama;tutar")]
    public void Headers_MatchRegardlessOfCasingAndTurkishLetters(string header)
    {
        var result = Parse($"{header}\n05.03.2026;Kira;-100\n");

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("05.03.2026")]
    [InlineData("5.3.2026")]
    [InlineData("05/03/2026")]
    [InlineData("2026-03-05")]
    [InlineData("05.03.2026 14:32")]
    public void Dates_InCommonExportFormats(string date)
    {
        var result = Parse($"Tarih;Açıklama;Tutar\n{date};Kira;-100\n");

        Assert.Equal(new DateOnly(2026, 3, 5), Assert.Single(result.Lines).Date);
    }

    [Fact]
    public void InvalidLines_AreReportedWithTheirLineNumbers()
    {
        var result = Parse("""
            Tarih;Açıklama;Tutar
            05.03.2026;Kira;-20.000,00
            31.02.2026;Geçersiz tarih;-10
            06.03.2026;;-10
            07.03.2026;Bozuk tutar;abc
            08.03.2026;Sıfır;0
            """);

        Assert.Single(result.Lines);
        Assert.Equal([3, 4, 5, 6], result.Errors.Select(e => e.LineNumber));
    }

    [Fact]
    public void BlankLines_AreSkipped()
    {
        var result = Parse("Tarih;Açıklama;Tutar\n\n05.03.2026;Kira;-100\n;;\n");

        Assert.True(result.IsValid);
        Assert.Single(result.Lines);
    }

    [Fact]
    public void MissingRequiredColumns_FailTheWholeFile()
    {
        var result = Parse("Tarih;Not;Tutar\n05.03.2026;Kira;-100\n");

        Assert.False(result.IsValid);
        Assert.Equal(1, Assert.Single(result.Errors).LineNumber);
        Assert.Empty(result.Lines);
    }

    [Fact]
    public void EmptyFile_IsRejected()
    {
        Assert.False(Parse("").IsValid);
    }

    [Fact]
    public void HeaderOnly_IsRejected()
    {
        Assert.False(Parse("Tarih;Açıklama;Tutar\n").IsValid);
    }
}
