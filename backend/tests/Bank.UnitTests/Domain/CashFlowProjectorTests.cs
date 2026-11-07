using Bank.Domain.Cards;
using Bank.Domain.CashFlows;
using Bank.Domain.Common;
using Bank.Domain.Projection;
using Bank.Domain.Scheduling;
using Bank.Domain.ValueObjects;

namespace Bank.UnitTests.Domain;

public sealed class CashFlowProjectorTests
{
    private const long CardId = 1;
    private static readonly CreditCard Card = new(CardId, "Bonus", Tl(100_000m), statementDay: 20, paymentDueDays: 10);

    private static DateOnly D(int year, int month, int day) => new(year, month, day);
    private static Money Tl(decimal amount) => new(amount, Currency.TRY);

    private static RecurringCashFlow Monthly(
        string name, CashFlowKind kind, decimal amount, DateOnly start, long? cardId = null, Currency? currency = null)
        => new(0, name, kind, "Genel", new Money(amount, currency ?? Currency.TRY), RecurrenceRule.Monthly(start), cardId);

    private static InstallmentPlan Laptop(DateOnly purchaseDate)
        => new(1, CardId, "Laptop", Tl(1000m), 3, purchaseDate);

    private static CashFlowProjection Project(
        DateOnly from, int months, decimal openingBalance = 0m,
        RecurringCashFlow[]? flows = null, CreditCard[]? cards = null, InstallmentPlan[]? plans = null)
        => CashFlowProjector.Project(new ProjectionInput(
            from, months, Tl(openingBalance), flows ?? [], cards ?? [Card], plans ?? []));

    [Fact]
    public void DirectItems_AppearOnTheirOwnDates()
    {
        var projection = Project(D(2026, 3, 1), 1, flows:
        [
            Monthly("Maaş", CashFlowKind.Income, 45_000m, D(2026, 1, 15)),
            Monthly("Kira", CashFlowKind.Expense, 20_000m, D(2026, 1, 1))
        ]);

        Assert.Equal(
            [(D(2026, 3, 1), "Kira", Tl(-20_000m)), (D(2026, 3, 15), "Maaş", Tl(45_000m))],
            projection.Entries.Select(e => (e.Date, e.Description, e.Amount)));
    }

    // A month can close positive while the account dips below zero before the salary arrives.
    // Month-end figures alone hide this; the lowest balance exposes it.
    [Fact]
    public void Month_ClosingPositive_CanStillDipBelowZeroMidMonth()
    {
        var march = Assert.Single(Project(D(2026, 3, 1), 1, openingBalance: 5_000m, flows:
        [
            Monthly("Maaş", CashFlowKind.Income, 45_000m, D(2026, 1, 15)),
            Monthly("Kira", CashFlowKind.Expense, 20_000m, D(2026, 1, 1))
        ]).Months);

        Assert.Equal(Tl(45_000m), march.Income);
        Assert.Equal(Tl(20_000m), march.Outflows);
        Assert.Equal(Tl(25_000m), march.Net);
        Assert.Equal(Tl(30_000m), march.ClosingBalance);
        Assert.Equal(Tl(-15_000m), march.LowestBalance);
        Assert.Equal(D(2026, 3, 1), march.LowestBalanceDate);
        Assert.False(march.IsDeficit);
        Assert.True(march.BalanceGoesNegative);
    }

    [Fact]
    public void SameDay_OutflowsAreAppliedBeforeIncome()
    {
        // Salary and rent on the same day: whether the account dips depends on which bank posts first.
        // The projection assumes the worse order so it never under-reports a shortfall.
        var march = Assert.Single(Project(D(2026, 3, 1), 1, flows:
        [
            Monthly("Maaş", CashFlowKind.Income, 10_000m, D(2026, 3, 1)),
            Monthly("Kira", CashFlowKind.Expense, 15_000m, D(2026, 3, 1))
        ]).Months);

        Assert.Equal(Tl(-15_000m), march.LowestBalance);
        Assert.Equal(Tl(-5_000m), march.ClosingBalance);
    }

    [Fact]
    public void CardSubscription_IsPaidOnStatementDueDate_NotOnChargeDate()
    {
        var projection = Project(D(2026, 3, 1), 3, flows:
        [
            Monthly("Netflix", CashFlowKind.Subscription, 229.99m, D(2026, 1, 31), CardId)
        ]);

        Assert.DoesNotContain(projection.Entries, e => e.Kind == ProjectionEntryKind.Subscription);
        Assert.Equal(
            // Charged 31 Jan, 28 Feb, 31 Mar → statements 20 Feb, 20 Mar, 20 Apr → due dates below.
            // The 30 Apr charge is due 30 May (Saturday) → 1 June, outside the horizon.
            [D(2026, 3, 2), D(2026, 3, 30), D(2026, 4, 30)],
            projection.Entries.Select(e => e.Date));
        Assert.All(projection.Entries, e => Assert.Equal(ProjectionEntryKind.CardPayment, e.Kind));
    }

    [Fact]
    public void ChargeMadeBeforeHorizon_IsIncludedWhenDueInsideIt()
    {
        // The 31 Jan charge belongs to the 20 Feb statement, due 2 March: before the horizon in
        // spending terms, inside it in cash terms.
        var first = Project(D(2026, 3, 1), 1, flows:
        [
            Monthly("Netflix", CashFlowKind.Subscription, 229.99m, D(2026, 1, 31), CardId)
        ]).Entries[0];

        Assert.Equal(D(2026, 3, 2), first.Date);
        Assert.Equal(D(2026, 2, 20), first.StatementDate);
    }

    [Fact]
    public void ChargesOnTheSameStatement_BecomeOnePaymentWithItsLines()
    {
        var projection = Project(D(2026, 4, 1), 1,
            flows: [Monthly("Netflix", CashFlowKind.Subscription, 229.99m, D(2026, 1, 31), CardId)],
            plans: [Laptop(D(2026, 3, 25))]);

        var payment = Assert.Single(projection.Entries);

        Assert.Equal(D(2026, 4, 30), payment.Date);
        Assert.Equal(Tl(-563.33m), payment.Amount);
        Assert.Equal(
            [new CardCharge("Netflix", Tl(229.99m)), new CardCharge("Laptop (1/3)", Tl(333.34m))],
            payment.Charges);
    }

    [Fact]
    public void WeekendShift_MovesAnInstallmentIntoTheFollowingMonth()
    {
        // The case found in sprint 1: May's due date falls on Saturday 30 May and moves to 1 June,
        // so May carries no card payment and June carries two.
        var months = Project(D(2026, 4, 1), 3, plans: [Laptop(D(2026, 3, 25))]).Months;

        Assert.Equal([Tl(333.34m), Tl(0m), Tl(666.66m)], months.Select(m => m.CardPayments));
    }

    [Fact]
    public void BurdenRatio_IsOutflowsOverIncome_AndNullWithoutIncome()
    {
        var months = Project(D(2026, 3, 1), 2, flows:
        [
            Monthly("Maaş", CashFlowKind.Income, 40_000m, D(2026, 3, 15)),
            Monthly("Kira", CashFlowKind.Expense, 10_000m, D(2026, 3, 1)),
            Monthly("Spor", CashFlowKind.Expense, 1_000m, D(2026, 3, 1))
        ]).Months;

        Assert.Equal(0.275m, months[0].BurdenRatio);

        var noIncome = Assert.Single(Project(D(2026, 3, 1), 1, flows:
        [
            Monthly("Kira", CashFlowKind.Expense, 10_000m, D(2026, 3, 1))
        ]).Months);
        Assert.Null(noIncome.BurdenRatio);
        Assert.True(noIncome.IsDeficit);
    }

    [Fact]
    public void Horizon_StartingMidMonth_ProducesPartialFirstAndLastMonths()
    {
        var projection = Project(D(2026, 3, 15), 3);

        Assert.Equal(D(2026, 6, 14), projection.To);
        Assert.Equal([3, 4, 5, 6], projection.Months.Select(m => m.Month));
    }

    [Fact]
    public void ClosingBalance_CarriesOverBetweenMonths()
    {
        var months = Project(D(2026, 3, 1), 3, openingBalance: 1_000m, flows:
        [
            Monthly("Kira", CashFlowKind.Expense, 400m, D(2026, 1, 5))
        ]).Months;

        Assert.Equal([Tl(600m), Tl(200m), Tl(-200m)], months.Select(m => m.ClosingBalance));
    }

    [Fact]
    public void ItemsInAnotherCurrency_AreExcludedAndReported()
    {
        var usdCard = new CreditCard(2, "USD kart", new Money(5_000m, Currency.USD), 20, 10);

        var projection = Project(D(2026, 3, 1), 1,
            flows: [Monthly("AWS", CashFlowKind.Expense, 50m, D(2026, 3, 3), currency: Currency.USD)],
            cards: [Card, usdCard],
            plans: [new InstallmentPlan(9, 2, "Kamera", new Money(900m, Currency.USD), 3, D(2026, 2, 1))]);

        Assert.Empty(projection.Entries);
        Assert.Equal(["AWS", "Kamera"], projection.ExcludedItems);
    }

    [Fact]
    public void ItemOnUnknownCard_IsRejected()
    {
        Assert.Throws<DomainException>(() => Project(D(2026, 3, 1), 1, cards: [],
            flows: [Monthly("Netflix", CashFlowKind.Subscription, 100m, D(2026, 1, 1), CardId)]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(CashFlowProjector.MaxMonths + 1)]
    public void Horizon_OutOfRange_IsRejected(int months)
    {
        Assert.Throws<DomainException>(() => Project(D(2026, 3, 1), months));
    }
}
