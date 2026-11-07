using Bank.Domain.Cards;
using Bank.Domain.CashFlows;
using Bank.Domain.Common;
using Bank.Domain.Projection;
using Bank.Domain.Scenarios;
using Bank.Domain.Scheduling;
using Bank.Domain.ValueObjects;

namespace Bank.UnitTests.Domain;

public sealed class ScenarioComparerTests
{
    private const long CardId = 1;

    // Statement on the 10th, due on the 20th: every installment is paid within its own month,
    // which keeps the arithmetic in these tests easy to follow.
    private static readonly CreditCard Card = new(CardId, "Bonus", Tl(100_000m), statementDay: 10, paymentDueDays: 10);

    private static readonly DateOnly From = new(2026, 4, 1);
    private static readonly DateOnly PurchaseDate = new(2026, 4, 5);

    private static Money Tl(decimal amount) => new(amount, Currency.TRY);

    /// <summary>
    /// Opening balance 1 000; salary 20 000 on the 1st, living costs 18 400 on the 2nd.
    /// The user saves 1 600 a month, so any installment above that eats into the balance.
    /// </summary>
    private static ProjectionInput TightBudget(int minimumMonths = 3) => new(
        From,
        minimumMonths,
        Tl(1_000m),
        [
            new RecurringCashFlow(1, "Maaş", CashFlowKind.Income, "Maaş", Tl(20_000m), RecurrenceRule.Monthly(new(2026, 1, 1))),
            new RecurringCashFlow(2, "Giderler", CashFlowKind.Expense, "Yaşam", Tl(18_400m), RecurrenceRule.Monthly(new(2026, 1, 2)))
        ],
        [Card],
        []);

    private static PurchaseScenario Laptop(params PurchaseOption[] options)
        => new(CardId, "Laptop", PurchaseDate, options);

    private static PurchaseOption Option(int count, decimal total = 9_000m) => new(count, Tl(total));

    [Fact]
    public void ShorterPlan_PushesTheBalanceBelowZero_WhileLongerPlanDoesNot()
    {
        var result = ScenarioComparer.Compare(TightBudget(), Laptop(Option(3), Option(6)));

        var threeInstallments = result.Options[0].Metrics;
        var sixInstallments = result.Options[1].Metrics;

        // 3 × 3 000 exceeds the 1 600 monthly surplus; the balance goes negative in April and only
        // recovers in September. 6 × 1 500 stays within it.
        Assert.Equal(5, threeInstallments.MonthsBelowZero);
        Assert.Equal(Tl(-3_200m), threeInstallments.LowestBalance);
        Assert.Equal(0, sixInstallments.MonthsBelowZero);
        Assert.Equal(0, result.Baseline.MonthsBelowZero);
    }

    [Fact]
    public void PeakMonthlyOutflow_ReflectsTheInstallmentSize()
    {
        var result = ScenarioComparer.Compare(TightBudget(), Laptop(Option(3), Option(6)));

        Assert.Equal(Tl(18_400m), result.Baseline.PeakMonthlyOutflow);
        Assert.Equal(Tl(21_400m), result.Options[0].Metrics.PeakMonthlyOutflow);
        Assert.Equal(Tl(19_900m), result.Options[1].Metrics.PeakMonthlyOutflow);
    }

    [Fact]
    public void AllOptions_AreComparedOverTheHorizonOfTheLongestOne()
    {
        // Asked for 3 months, but the 6-installment plan runs until September.
        var result = ScenarioComparer.Compare(TightBudget(minimumMonths: 3), Laptop(Option(3), Option(6)));

        Assert.Equal(new DateOnly(2026, 9, 30), result.To);
        Assert.All(result.Options, o => Assert.Equal(6, o.Metrics.Months.Count));
        Assert.Equal(6, result.Baseline.Months.Count);
    }

    [Fact]
    public void SamePrice_LeavesTheSameBalanceOnceEveryPlanIsPaidOff()
    {
        var result = ScenarioComparer.Compare(TightBudget(), Laptop(Option(3), Option(6)));

        Assert.Equal(result.Options[0].Metrics.ClosingBalance, result.Options[1].Metrics.ClosingBalance);
        Assert.Equal(result.Baseline.ClosingBalance - Tl(9_000m), result.Options[0].Metrics.ClosingBalance);
    }

    [Fact]
    public void HigherPriceForMoreInstallments_ShowsUpInTheClosingBalance()
    {
        // A typical offer: 6 installments cost 5% more than 3.
        var result = ScenarioComparer.Compare(TightBudget(), Laptop(Option(3, 9_000m), Option(6, 9_450m)));

        Assert.Equal(Tl(450m), result.Options[0].Metrics.ClosingBalance - result.Options[1].Metrics.ClosingBalance);
    }

    [Fact]
    public void Installments_AreReportedPerOption()
    {
        var result = ScenarioComparer.Compare(TightBudget(), Laptop(Option(3, 1_000m)));

        Assert.Equal([Tl(333.34m), Tl(333.33m), Tl(333.33m)], result.Options[0].Installments.Select(i => i.Amount));
    }

    [Fact]
    public void Comparison_DoesNotChangeTheCallersInput()
    {
        var input = TightBudget();

        ScenarioComparer.Compare(input, Laptop(Option(3)));

        Assert.Empty(input.InstallmentPlans);
    }

    [Fact]
    public void NoOptions_IsRejected()
    {
        Assert.Throws<DomainException>(() => ScenarioComparer.Compare(TightBudget(), Laptop()));
    }

    [Fact]
    public void TooManyOptions_IsRejected()
    {
        var options = Enumerable.Range(1, ScenarioComparer.MaxOptions + 1).Select(n => Option(n)).ToArray();

        Assert.Throws<DomainException>(() => ScenarioComparer.Compare(TightBudget(), Laptop(options)));
    }

    [Fact]
    public void DuplicateOptions_AreRejected()
    {
        Assert.Throws<DomainException>(() => ScenarioComparer.Compare(TightBudget(), Laptop(Option(3), Option(3))));
    }

    [Fact]
    public void UnknownCard_IsRejected()
    {
        var scenario = new PurchaseScenario(99, "Laptop", PurchaseDate, [Option(3)]);

        Assert.Throws<DomainException>(() => ScenarioComparer.Compare(TightBudget(), scenario));
    }

    [Fact]
    public void PurchaseInThePast_IsRejected()
    {
        var scenario = new PurchaseScenario(CardId, "Laptop", From.AddDays(-1), [Option(3)]);

        Assert.Throws<DomainException>(() => ScenarioComparer.Compare(TightBudget(), scenario));
    }

    [Fact]
    public void OptionBeyondTheMaximumHorizon_IsRejected()
    {
        Assert.Throws<DomainException>(() => ScenarioComparer.Compare(TightBudget(), Laptop(Option(36))));
    }
}
