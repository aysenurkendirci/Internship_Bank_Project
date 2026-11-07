using Bank.Application.Common;

namespace Bank.UnitTests.Application;

public sealed class PersonalDataTests
{
    [Fact]
    public void IdentityNumber_KeepsOnlyTheFirstThreeAndLastTwoDigits()
    {
        Assert.Equal("100******46", PersonalData.MaskTcKimlikNo("10000000146"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    public void TooShortToMaskMeaningfully_RevealsNothing(string? value)
    {
        Assert.Equal("***", PersonalData.MaskTcKimlikNo(value));
    }
}
