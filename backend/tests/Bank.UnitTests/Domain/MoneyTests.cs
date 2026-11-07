using System.Globalization;
using Bank.Domain.Common;
using Bank.Domain.ValueObjects;

namespace Bank.UnitTests.Domain;

public sealed class MoneyTests
{
    private static Money Tl(decimal amount) => new(amount, Currency.TRY);
    private static Money Usd(decimal amount) => new(amount, Currency.USD);
    private static decimal Parse(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    [Fact]
    public void Add_SameCurrency_SumsAmounts()
    {
        Assert.Equal(Tl(150.75m), Tl(100m) + Tl(50.75m));
    }

    [Fact]
    public void Add_DifferentCurrencies_IsRejected()
    {
        Assert.Throws<DomainException>(() => Tl(100m) + Usd(100m));
    }

    [Fact]
    public void Compare_DifferentCurrencies_IsRejected()
    {
        Assert.Throws<DomainException>(() => Tl(100m) > Usd(1m));
    }

    [Fact]
    public void Equality_IgnoresTrailingZeros()
    {
        Assert.Equal(Tl(10.5m), Tl(10.50m));
    }

    [Fact]
    public void Equality_DifferentCurrencies_AreNotEqual()
    {
        Assert.NotEqual(Tl(10m), Usd(10m));
    }

    [Theory]
    [InlineData("2.345", "2.35")]   // banker's rounding: 2.34
    [InlineData("0.125", "0.13")]   // banker's rounding: 0.12
    [InlineData("-2.345", "-2.35")]
    [InlineData("2.344", "2.34")]
    public void Round_MidpointsGoAwayFromZero(string amount, string expected)
    {
        var rounded = Tl(Parse(amount)).Round();

        Assert.Equal(Parse(expected), rounded.Amount);
    }

    [Fact]
    public void Arithmetic_KeepsDecimalPrecision()
    {
        // 0.1 + 0.2 is not 0.3 in binary floating point; decimal must get it exactly right.
        Assert.Equal(Tl(0.3m), Tl(0.1m) + Tl(0.2m));
    }

    [Theory]
    [InlineData("try")]
    [InlineData("TRY")]
    public void Currency_FromCode_IsCaseInsensitive(string code)
    {
        Assert.Same(Currency.TRY, Currency.FromCode(code));
    }

    [Fact]
    public void Currency_Unsupported_IsRejected()
    {
        // JPY has no minor unit; supporting it would break the "round to cents" rule.
        Assert.Throws<DomainException>(() => Currency.FromCode("JPY"));
    }

    [Fact]
    public void ToString_UsesInvariantFormat()
    {
        Assert.Equal("1234.50 TRY", Tl(1234.5m).ToString());
    }
}
