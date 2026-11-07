using System.Globalization;
using Bank.Application.Imports;

namespace Bank.UnitTests.Imports;

public sealed class StatementAmountTests
{
    [Theory]
    [InlineData("1.234,56", "1234.56")]       // Turkish
    [InlineData("-1.234,56", "-1234.56")]
    [InlineData("1.234.567,89", "1234567.89")]
    [InlineData("1,234.56", "1234.56")]       // international
    [InlineData("1234.5", "1234.5")]
    [InlineData("0,50", "0.50")]
    [InlineData("1.234", "1234")]             // three digits after the dot: thousands, not decimals
    [InlineData("12,345", "12345")]
    [InlineData("-250,00 TL", "-250.00")]
    [InlineData("₺ 1.500,00", "1500.00")]
    [InlineData("1 500,00", "1500.00")]
    [InlineData("1\u00A0500,00", "1500.00")]  // Excel's non-breaking space as thousands separator
    [InlineData("250,00-", "-250.00")]        // trailing minus used by some bank exports
    [InlineData("+75", "75")]
    public void Parses(string raw, string expected)
    {
        Assert.True(StatementAmount.TryParse(raw, out var amount));
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), amount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12,3,4")]
    [InlineData("1.2.3")]
    [InlineData("1,234.5.6")]
    public void RejectsWhatIsNotAnAmount(string? raw)
    {
        Assert.False(StatementAmount.TryParse(raw, out _));
    }
}
