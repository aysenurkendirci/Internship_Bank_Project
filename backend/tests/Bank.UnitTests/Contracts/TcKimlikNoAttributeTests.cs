using Bank.Contracts.Validation;

namespace Bank.UnitTests.Contracts;

public sealed class TcKimlikNoAttributeTests
{
    private readonly TcKimlikNoAttribute _sut = new();

    [Theory]
    [InlineData("10000000146")]
    [InlineData("12345678950")]
    [InlineData("11111111110")]
    public void ValidNumbers_Pass(string tcNo)
    {
        Assert.True(_sut.IsValid(tcNo));
    }

    [Theory]
    [InlineData("12345678901")]   // right length, wrong check digits
    [InlineData("10000000147")]   // 11th digit off by one
    [InlineData("01234567890")]   // leading zero
    [InlineData("1234567895")]    // 10 digits
    [InlineData("123456789500")]  // 12 digits
    [InlineData("1234567895a")]
    [InlineData("١٢٣٤٥٦٧٨٩٥٠")]   // non-ASCII digits must not slip through char.IsDigit
    [InlineData("")]
    public void InvalidNumbers_Fail(string tcNo)
    {
        Assert.False(_sut.IsValid(tcNo));
    }

    [Fact]
    public void Null_IsLeftToRequired()
    {
        Assert.True(_sut.IsValid(null));
    }
}
