using Bank.Domain.Cards;
using Bank.Domain.Common;
using Bank.Domain.ValueObjects;

namespace Bank.UnitTests.Domain;

public sealed class CreditCardTests
{
    private static DateOnly D(int year, int month, int day) => new(year, month, day);

    private static CreditCard Card(int statementDay, int paymentDueDays = 10)
        => new(1, "Bonus", new Money(50_000m, Currency.TRY), statementDay, paymentDueDays);

    [Fact]
    public void Purchase_BeforeStatementDay_GoesOnThisMonthsStatement()
    {
        Assert.Equal(D(2026, 3, 20), Card(statementDay: 20).StatementDateFor(D(2026, 3, 10)));
    }

    [Fact]
    public void Purchase_OnStatementDay_GoesOnThatStatement()
    {
        Assert.Equal(D(2026, 3, 20), Card(statementDay: 20).StatementDateFor(D(2026, 3, 20)));
    }

    [Fact]
    public void Purchase_AfterStatementDay_GoesOnNextMonthsStatement()
    {
        Assert.Equal(D(2026, 4, 20), Card(statementDay: 20).StatementDateFor(D(2026, 3, 21)));
    }

    [Fact]
    public void Purchase_AfterDecemberStatement_GoesOnJanuaryStatementOfNextYear()
    {
        Assert.Equal(D(2027, 1, 20), Card(statementDay: 20).StatementDateFor(D(2026, 12, 25)));
    }

    [Fact]
    public void StatementDay31_ClosesOnLastDayOfShortMonthsWithoutDrifting()
    {
        var card = Card(statementDay: 31);

        var february = card.StatementDateFor(D(2026, 2, 10));
        var march = card.NextStatementDate(february);
        var april = card.NextStatementDate(march);

        Assert.Equal([D(2026, 2, 28), D(2026, 3, 31), D(2026, 4, 30)], [february, march, april]);
    }

    [Fact]
    public void DueDate_OnWeekday_IsStatementPlusDueDays()
    {
        // 20 Mar 2026 + 10 days = Monday 30 Mar
        Assert.Equal(D(2026, 3, 30), Card(statementDay: 20).DueDateFor(D(2026, 3, 20)));
    }

    [Theory]
    [InlineData(15)]   // 15 Apr + 10 = Saturday 25 Apr
    [InlineData(16)]   // 16 Apr + 10 = Sunday 26 Apr
    public void DueDate_OnWeekend_MovesToMonday(int statementDay)
    {
        var card = Card(statementDay);

        Assert.Equal(D(2026, 4, 27), card.DueDateFor(D(2026, 4, statementDay)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void InvalidStatementDay_IsRejected(int statementDay)
    {
        Assert.Throws<DomainException>(() => Card(statementDay));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void InvalidPaymentDueDays_IsRejected(int paymentDueDays)
    {
        Assert.Throws<DomainException>(() => Card(statementDay: 20, paymentDueDays));
    }

    [Fact]
    public void NonPositiveLimit_IsRejected()
    {
        Assert.Throws<DomainException>(() => new CreditCard(1, "Kart", Money.Zero(Currency.TRY), 20, 10));
    }
}
