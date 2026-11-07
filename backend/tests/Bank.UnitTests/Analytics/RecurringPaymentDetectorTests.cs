using Bank.Application.Synthetic;
using Bank.Domain.Analytics;
using Bank.Domain.Common;

namespace Bank.UnitTests.Analytics;

public sealed class RecurringPaymentDetectorTests
{
    private static HistoryEntry On(int month, int day, string description, decimal amount, string? category = null)
        => new(new DateOnly(2026, month, day), description, amount, category);

    private static IEnumerable<HistoryEntry> Monthly(string description, decimal amount, int day = 5, int months = 6)
        => Enumerable.Range(1, months).Select(m => On(m, day, description, amount, "Abonelik"));

    [Fact]
    public void FixedMonthlyCharge_IsASubscription()
    {
        var found = Assert.Single(RecurringPaymentDetector.Detect(Monthly("NETFLIX.COM", -229.99m)));

        Assert.Equal(RecurringPattern.Fixed, found.Pattern);
        Assert.Equal(229.99m, found.TypicalAmount);
        Assert.Equal(5, found.TypicalDay);
        Assert.Equal(6, found.MonthsObserved);
        Assert.Equal(new DateOnly(2026, 7, 5), found.NextExpected);
        Assert.False(found.IsIncome);
    }

    [Fact]
    public void SameDayVaryingAmount_IsABill()
    {
        decimal[] amounts = [-620m, -710m, -580m, -800m];
        var history = amounts.Select((a, i) => On(i + 1, 10, "ENERJİSA ELEKTRİK", a));

        Assert.Equal(RecurringPattern.Variable, Assert.Single(RecurringPaymentDetector.Detect(history)).Pattern);
    }

    [Fact]
    public void Salary_IsDetectedAsRecurringIncome_DespiteWeekendShifts()
    {
        int[] paydays = [15, 13, 15, 14, 15, 15];
        var history = paydays.Select((d, i) => On(i + 1, d, "MAAŞ ÖDEMESİ", 45_000m));

        var found = Assert.Single(RecurringPaymentDetector.Detect(history));
        Assert.True(found.IsIncome);
        Assert.Equal(15, found.TypicalDay);
    }

    [Fact]
    public void PriceIncrease_IsStillDetected_AsVariable()
    {
        // A subscription that went up mid-year must not disappear from the list.
        var history = Monthly("NETFLIX.COM", -229.99m, months: 3)
            .Concat(Enumerable.Range(4, 3).Select(m => On(m, 5, "NETFLIX.COM", -279.99m)));

        Assert.Equal(RecurringPattern.Variable, Assert.Single(RecurringPaymentDetector.Detect(history)).Pattern);
    }

    [Fact]
    public void OneSkippedMonth_IsTolerated()
    {
        var history = Monthly("SPOTIFY", -99.99m, months: 8).Where(e => e.Date.Month != 4);

        Assert.Single(RecurringPaymentDetector.Detect(history));
    }

    [Fact]
    public void SpellingVariantsOfAMerchant_AreOneSeries()
    {
        var history = new[]
        {
            On(1, 3, "MİGROS SANAL MARKET ÜYELİK", -49m),
            On(2, 3, "MIGROS SANAL MARKET UYELIK", -49m),
            On(3, 3, "Migros Sanal Market Üyelik", -49m)
        };

        Assert.Single(RecurringPaymentDetector.Detect(history));
    }

    [Fact]
    public void SeveralTimesAMonth_IsNotRecurring()
    {
        var history = Enumerable.Range(1, 4).SelectMany(m => new[] { On(m, 3, "A101", -400m), On(m, 17, "A101", -650m) });

        Assert.Empty(RecurringPaymentDetector.Detect(history));
    }

    [Fact]
    public void MonthlyButOnRandomDays_IsNotRecurring()
    {
        int[] days = [2, 27, 9, 21, 14];
        var history = days.Select((d, i) => On(i + 1, d, "STARBUCKS", -150m));

        Assert.Empty(RecurringPaymentDetector.Detect(history));
    }

    [Fact]
    public void FewerThanThreeMonths_IsNotEnoughEvidence()
    {
        Assert.Empty(RecurringPaymentDetector.Detect(Monthly("NETFLIX.COM", -229.99m, months: 2)));
    }

    [Fact]
    public void RefundAndRechargeInTheSameMonth_IsAKnownLimitation()
    {
        // Documented limitation: a second charge in one month breaks the "at most once a month" rule.
        var history = Monthly("NETFLIX.COM", -229.99m).Append(On(3, 20, "NETFLIX.COM", -229.99m));

        Assert.Empty(RecurringPaymentDetector.Detect(history));
    }

    /// <summary>
    /// Evaluation on held-out synthetic profiles. Thresholds were tuned on seeds 1–10; seeds 101–110 were
    /// not looked at before this test was written. The acceptance bar (≥ 0.95) was fixed in advance.
    /// </summary>
    [Fact]
    public void HeldOutSyntheticProfiles_PrecisionAndRecallAreAtLeast95Percent()
    {
        var truth = new[]
        {
            "KİRA ÖDEMESİ", "MACFIT SPOR SALONU", "NETFLIX.COM", "SPOTIFY", "TÜRK TELEKOM İNTERNET",
            "MAAŞ ÖDEMESİ", "ENERJİSA ELEKTRİK", "İGDAŞ DOĞALGAZ", "İSKİ SU"
        }.Select(TextFolding.Fold).ToHashSet();

        int truePositives = 0, falsePositives = 0, falseNegatives = 0;
        foreach (var seed in Enumerable.Range(101, 10))
        {
            var history = SyntheticHistoryGenerator.Generate(new(2025, 1, 1), new(2025, 12, 31), seed)
                .Select(t => new HistoryEntry(t.Date, t.Description, t.Amount, t.Category));
            var found = RecurringPaymentDetector.Detect(history).Select(p => TextFolding.Fold(p.Merchant)).ToHashSet();

            truePositives += found.Intersect(truth).Count();
            falsePositives += found.Except(truth).Count();
            falseNegatives += truth.Except(found).Count();
        }

        var precision = (double)truePositives / (truePositives + falsePositives);
        var recall = (double)truePositives / (truePositives + falseNegatives);

        Assert.True(precision >= 0.95, $"precision {precision:0.000}");
        Assert.True(recall >= 0.95, $"recall {recall:0.000}");
    }
}
