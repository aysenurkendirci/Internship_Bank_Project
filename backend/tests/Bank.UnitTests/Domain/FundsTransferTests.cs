using Bank.Domain.Accounts;
using Bank.Domain.Common;
using Bank.Domain.ValueObjects;

namespace Bank.UnitTests.Domain;

public sealed class FundsTransferTests
{
    private static Money Try(decimal amount) => new(amount, Currency.TRY);

    private static Account NewAccount(long id, decimal balance, string holder = "Ayşe Yılmaz",
        AccountStatus status = AccountStatus.Active, Currency? currency = null)
        => new(id, userId: id * 10, Iban.For("99999", id), holder, status, new Money(balance, currency ?? Currency.TRY));

    [Fact]
    public void MovesTheAmount_AndPostingsCancelOut()
    {
        var source = NewAccount(1, 1_000m);
        var target = NewAccount(2, 50m, holder: "Mehmet Demir");

        var postings = FundsTransfer.Execute(source, target, Try(250.10m), "Mehmet Demir");

        Assert.Equal(Try(749.90m), source.Balance);
        Assert.Equal(Try(300.10m), target.Balance);
        Assert.Equal(postings.Debit.Amount, postings.Credit.Amount);
        Assert.Equal(Try(749.90m), postings.Debit.BalanceAfter);
        Assert.Equal(Try(300.10m), postings.Credit.BalanceAfter);
    }

    [Fact]
    public void TheWholeBalance_CanBeSent()
    {
        var source = NewAccount(1, 100m);

        FundsTransfer.Execute(source, NewAccount(2, 0m), Try(100m), "Ayşe Yılmaz");

        Assert.True(source.Balance.IsZero);
    }

    [Fact]
    public void MoreThanTheBalance_IsRefused_AndNothingMoves()
    {
        var source = NewAccount(1, 100m);
        var target = NewAccount(2, 0m);

        Assert.Throws<DomainException>(() => FundsTransfer.Execute(source, target, Try(100.01m), "Ayşe Yılmaz"));

        Assert.Equal(Try(100m), source.Balance);
        Assert.Equal(Try(0m), target.Balance);
    }

    [Theory]
    [InlineData("AYŞE YILMAZ")]
    [InlineData("ayse  yilmaz")]
    [InlineData(" Ayşe Yılmaz ")]
    public void RecipientName_IgnoresCaseSpacingAndTurkishLetters(string typed)
    {
        FundsTransfer.Execute(NewAccount(1, 100m), NewAccount(2, 0m), Try(10m), typed);
    }

    [Fact]
    public void RecipientName_MustBelongToTheIbanHolder()
    {
        var error = Assert.Throws<DomainException>(
            () => FundsTransfer.Execute(NewAccount(1, 100m), NewAccount(2, 0m), Try(10m), "Ayşe Kaya"));

        Assert.Contains("eşleşmiyor", error.Message);
    }

    [Fact]
    public void SameAccount_IsRefused()
    {
        var account = NewAccount(1, 100m);

        Assert.Throws<DomainException>(() => FundsTransfer.Execute(account, account, Try(10m), "Ayşe Yılmaz"));
    }

    [Fact]
    public void DifferentCurrencies_AreRefused()
    {
        var usd = NewAccount(2, 0m, currency: Currency.USD);

        Assert.Throws<DomainException>(() => FundsTransfer.Execute(NewAccount(1, 100m), usd, Try(10m), "Ayşe Yılmaz"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(0.001)]
    public void AmountMustBePositiveWholeCents(decimal amount)
    {
        Assert.Throws<DomainException>(
            () => FundsTransfer.Execute(NewAccount(1, 100m), NewAccount(2, 0m), Try(amount), "Ayşe Yılmaz"));
    }

    [Theory]
    [InlineData(AccountStatus.Frozen, AccountStatus.Active)]
    [InlineData(AccountStatus.Active, AccountStatus.Frozen)]
    [InlineData(AccountStatus.Closed, AccountStatus.Active)]
    [InlineData(AccountStatus.Active, AccountStatus.Closed)]
    public void FrozenOrClosedAccounts_CanNeitherSendNorReceive(AccountStatus sourceStatus, AccountStatus targetStatus)
    {
        var source = NewAccount(1, 100m, status: sourceStatus);
        var target = NewAccount(2, 0m, status: targetStatus);

        Assert.Throws<DomainException>(() => FundsTransfer.Execute(source, target, Try(10m), "Ayşe Yılmaz"));
        Assert.Equal(Try(100m), source.Balance);
    }
}
