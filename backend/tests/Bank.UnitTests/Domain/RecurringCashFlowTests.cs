using Bank.Domain.CashFlows;
using Bank.Domain.Common;
using Bank.Domain.Scheduling;
using Bank.Domain.ValueObjects;

namespace Bank.UnitTests.Domain;

public sealed class RecurringCashFlowTests
{
    private static readonly DateOnly Start = new(2026, 1, 15);
    private static readonly RecurrenceRule MonthlyFromStart = RecurrenceRule.Monthly(Start);

    private static RecurringCashFlow Create(
        CashFlowKind kind, decimal amount = 100m, long? creditCardId = null, string name = "Kalem")
        => new(1, name, kind, "Genel", new Money(amount, Currency.TRY), MonthlyFromStart, creditCardId);

    [Fact]
    public void Income_OccurrencesArePositive()
    {
        var salary = Create(CashFlowKind.Income, 45_000m);

        var occurrences = salary.OccurrencesBetween(Start, Start.AddMonths(2)).ToList();

        Assert.Equal(3, occurrences.Count);
        Assert.All(occurrences, o => Assert.Equal(new Money(45_000m, Currency.TRY), o.NetAmount));
    }

    [Theory]
    [InlineData(CashFlowKind.Expense)]
    [InlineData(CashFlowKind.Subscription)]
    public void Outflow_OccurrencesAreNegative(CashFlowKind kind)
    {
        var item = Create(kind, 250m);

        var occurrence = Assert.Single(item.OccurrencesBetween(Start, Start));

        Assert.Equal(new Money(-250m, Currency.TRY), occurrence.NetAmount);
    }

    [Fact]
    public void Subscription_CanBeChargedToCard()
    {
        var streaming = Create(CashFlowKind.Subscription, creditCardId: 3);

        Assert.True(streaming.IsChargedToCard);
    }

    [Fact]
    public void Income_ChargedToCard_IsRejected()
    {
        Assert.Throws<DomainException>(() => Create(CashFlowKind.Income, creditCardId: 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void NonPositiveAmount_IsRejected(decimal amount)
    {
        // Direction comes from Kind; a negative amount would flip it a second time.
        Assert.Throws<DomainException>(() => Create(CashFlowKind.Expense, amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankName_IsRejected(string name)
    {
        Assert.Throws<DomainException>(() => Create(CashFlowKind.Expense, name: name));
    }

    [Fact]
    public void Name_IsTrimmed()
    {
        Assert.Equal("Kira", Create(CashFlowKind.Expense, name: "  Kira ").Name);
    }
}
