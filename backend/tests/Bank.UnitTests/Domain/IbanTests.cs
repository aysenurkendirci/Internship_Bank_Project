using Bank.Domain.Accounts;
using Bank.Domain.Common;

namespace Bank.UnitTests.Domain;

public sealed class IbanTests
{
    private const string Valid = "TR330006100519786457841326";

    [Theory]
    [InlineData(Valid)]
    [InlineData("TR33 0006 1005 1978 6457 8413 26")]
    [InlineData("tr330006100519786457841326")]
    public void PrintedAndCompactForms_AreAccepted(string text)
    {
        Assert.Equal(Valid, Iban.Parse(text).Value);
    }

    [Theory]
    [InlineData("TR330006100519786457841327")] // one digit off
    [InlineData("TR330006100519786457841362")] // last two digits swapped
    [InlineData("TR33000610051978645784132")] // too short
    [InlineData("DE89370400440532013000")] // valid, but not Turkish
    [InlineData("TR33000610051978645784132X")]
    [InlineData("")]
    [InlineData(null)]
    public void Mistakes_AreRejected(string? text)
    {
        Assert.False(Iban.TryParse(text, out _));
        Assert.Throws<DomainException>(() => Iban.Parse(text));
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(42L)]
    [InlineData(9_999_999_999_999_999L)]
    public void GeneratedIbans_PassTheirOwnCheck(long accountNumber)
    {
        var iban = Iban.For("99999", accountNumber);

        Assert.True(Iban.TryParse(iban.Value, out _));
        Assert.EndsWith(accountNumber.ToString("D16"), iban.Value);
    }

    [Fact]
    public void Generated_MatchesAHandComputedExample()
    {
        Assert.Equal("TR809999900000000000000001", Iban.For("99999", 1).Value);
    }

    [Fact]
    public void Formatted_GroupsByFour()
    {
        Assert.Equal("TR33 0006 1005 1978 6457 8413 26", Iban.Parse(Valid).Formatted);
    }
}
