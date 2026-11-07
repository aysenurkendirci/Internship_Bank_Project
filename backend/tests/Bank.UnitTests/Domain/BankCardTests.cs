using Bank.Domain.Cards;
using Bank.Domain.Common;

namespace Bank.UnitTests.Domain;

public sealed class BankCardTests
{
    private static BankCard NewCard(BankCardStatus status = BankCardStatus.Active)
        => new(id: 1, userId: 7, status, contactless: true, onlineUse: true, dailyLimit: 5_000m, monthlyLimit: 50_000m);

    [Fact]
    public void Settings_CanBeChanged()
    {
        var card = NewCard();

        card.ChangeSettings(contactless: false, onlineUse: false, dailyLimit: 1_000m, monthlyLimit: 1_000m);

        Assert.Equal((false, false, 1_000m, 1_000m), (card.Contactless, card.OnlineUse, card.DailyLimit, card.MonthlyLimit));
    }

    [Theory]
    [InlineData(-1, 100)]
    [InlineData(2_000, 1_999)]
    public void InvalidLimits_AreRefused_AndNothingChanges(decimal daily, decimal monthly)
    {
        var card = NewCard();

        Assert.Throws<DomainException>(() => card.ChangeSettings(false, false, daily, monthly));
        Assert.Equal((true, 5_000m), (card.Contactless, card.DailyLimit));
    }

    [Fact]
    public void BlockedCard_CanBeOpenedAgain()
    {
        var card = NewCard();

        card.Block();
        Assert.Equal(BankCardStatus.Blocked, card.Status);

        card.Unblock();
        Assert.Equal(BankCardStatus.Active, card.Status);
    }

    [Fact]
    public void CancelledCard_CannotBeChangedOrRevived()
    {
        var card = NewCard(BankCardStatus.Cancelled);

        Assert.Throws<DomainException>(card.Unblock);
        Assert.Throws<DomainException>(card.Block);
        Assert.Throws<DomainException>(() => card.ChangeSettings(true, true, 1m, 1m));
        Assert.Equal(BankCardStatus.Cancelled, card.Status);
    }
}
