using Bank.Domain.Cards;
using Bank.Domain.Common;
using Bank.Domain.ValueObjects;

namespace Bank.UnitTests.Domain;

public sealed class InstallmentPlanTests
{
    private const long CardId = 1;

    private static readonly CreditCard Card = new(CardId, "Bonus", Tl(100_000m), statementDay: 20, paymentDueDays: 10);

    private static DateOnly D(int year, int month, int day) => new(year, month, day);
    private static Money Tl(decimal amount) => new(amount, Currency.TRY);

    private static InstallmentPlan Plan(decimal total, int count, DateOnly purchaseDate, long cardId = CardId)
        => new(1, cardId, "Dizüstü bilgisayar", Tl(total), count, purchaseDate);

    [Fact]
    public void Schedule_FollowsCardStatements()
    {
        // Bought after the March statement closed, so the first installment is on April's statement.
        var schedule = Plan(1000m, 3, D(2026, 3, 25)).ScheduleOn(Card);

        Assert.Equal(
            [
                new Installment(1, D(2026, 4, 20), D(2026, 4, 30), Tl(333.34m)),
                // 30 May is a Saturday, so the due date moves to Monday 1 June.
                new Installment(2, D(2026, 5, 20), D(2026, 6, 1), Tl(333.33m)),
                new Installment(3, D(2026, 6, 20), D(2026, 6, 30), Tl(333.33m))
            ],
            schedule);
    }

    [Fact]
    public void Schedule_WeekendShift_CanPutTwoPaymentsInOneMonth()
    {
        // The pile-up this project is meant to surface: June carries two installment payments.
        var schedule = Plan(1000m, 3, D(2026, 3, 25)).ScheduleOn(Card);

        Assert.Equal(2, schedule.Count(i => i.DueDate.Month == 6));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(9)]
    [InlineData(12)]
    public void Schedule_AmountsAlwaysAddUpToTotal(int count)
    {
        var schedule = Plan(32_999.99m, count, D(2026, 1, 5)).ScheduleOn(Card);

        Assert.Equal(count, schedule.Count);
        Assert.Equal(Tl(32_999.99m), schedule.Select(i => i.Amount).Aggregate((a, b) => a + b));
    }

    [Fact]
    public void Schedule_SinglePayment_IsTheWholeAmountOnFirstStatement()
    {
        var installment = Assert.Single(Plan(4_500m, 1, D(2026, 3, 10)).ScheduleOn(Card));

        Assert.Equal(new Installment(1, D(2026, 3, 20), D(2026, 3, 30), Tl(4_500m)), installment);
    }

    [Fact]
    public void Schedule_OnAnotherCard_IsRejected()
    {
        Assert.Throws<DomainException>(() => Plan(1000m, 3, D(2026, 3, 25), cardId: 99).ScheduleOn(Card));
    }

    [Fact]
    public void Schedule_InDifferentCurrencyThanCard_IsRejected()
    {
        var usdPlan = new InstallmentPlan(1, CardId, "Kulaklık", new Money(300m, Currency.USD), 3, D(2026, 3, 1));

        Assert.Throws<DomainException>(() => usdPlan.ScheduleOn(Card));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(InstallmentPlan.MaxInstallmentCount + 1)]
    public void InvalidInstallmentCount_IsRejected(int count)
    {
        Assert.Throws<DomainException>(() => Plan(1000m, count, D(2026, 3, 1)));
    }
}
