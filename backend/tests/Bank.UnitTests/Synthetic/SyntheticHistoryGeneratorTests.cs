using Bank.Application.Synthetic;

namespace Bank.UnitTests.Synthetic;

public sealed class SyntheticHistoryGeneratorTests
{
    private static readonly DateOnly From = new(2025, 1, 1);
    private static readonly DateOnly To = new(2025, 12, 31);

    private static IReadOnlyList<SyntheticTransaction> Year(int seed = 42) => SyntheticHistoryGenerator.Generate(From, To, seed);

    [Fact]
    public void SameSeed_ProducesTheSameHistory()
    {
        Assert.Equal(Year(seed: 42), Year(seed: 42));
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentHistories()
    {
        Assert.NotEqual(Year(seed: 42), Year(seed: 7));
    }

    [Fact]
    public void History_StaysWithinTheRangeAndIsOrdered()
    {
        var history = SyntheticHistoryGenerator.Generate(new DateOnly(2025, 3, 10), new DateOnly(2025, 5, 20), seed: 1);

        Assert.All(history, t => Assert.InRange(t.Date, new DateOnly(2025, 3, 10), new DateOnly(2025, 5, 20)));
        Assert.Equal(history.OrderBy(t => t.Date), history);
    }

    [Fact]
    public void Amounts_AreNonZeroWholeCents()
    {
        Assert.All(Year(), t =>
        {
            Assert.NotEqual(0m, t.Amount);
            Assert.Equal(Math.Round(t.Amount, 2), t.Amount);
        });
    }

    [Fact]
    public void Salary_ArrivesOncePerMonth_OnTheLastWorkingDayUpToThe15th()
    {
        var salaries = Year().Where(t => t.Category == "Maaş").ToList();

        Assert.Equal(12, salaries.Count);
        Assert.All(salaries, s =>
        {
            Assert.InRange(s.Date.Day, 13, 15);
            Assert.DoesNotContain(s.Date.DayOfWeek, new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });
        });
        // 15 February 2025 was a Saturday.
        Assert.Contains(salaries, s => s.Date == new DateOnly(2025, 2, 14));
    }

    [Fact]
    public void Subscriptions_RepeatWithTheSameAmountOnTheSameDay()
    {
        var netflix = Year().Where(t => t.Description == "NETFLIX.COM").ToList();

        Assert.Equal(12, netflix.Count);
        Assert.All(netflix, n => Assert.Equal((5, -229.99m), (n.Date.Day, n.Amount)));
    }

    [Fact]
    public void HeatingBill_IsHigherInWinterThanInSummer()
    {
        var gas = Year().Where(t => t.Description == "İGDAŞ DOĞALGAZ").ToDictionary(t => t.Date.Month, t => -t.Amount);

        Assert.True(gas[1] > 3 * gas[7]);
    }

    [Fact]
    public void EverydaySpending_RisesWithInflation()
    {
        var groceries = Year().Where(t => t.Category == "Market").ToList();
        decimal Average(int fromMonth, int toMonth)
            => groceries.Where(g => g.Date.Month >= fromMonth && g.Date.Month <= toMonth).Average(g => -g.Amount);

        Assert.True(Average(10, 12) > Average(1, 3));
    }

    [Fact]
    public void InjectedAnomalies_AreFlaggedAndUnusuallyLarge()
    {
        var history = Year();
        var anomalies = history.Where(t => t.IsAnomaly).ToList();
        var largestRegularSpend = history.Where(t => !t.IsAnomaly && t.Category is "Market" or "Restoran").Max(t => -t.Amount);

        Assert.NotEmpty(anomalies);
        Assert.All(anomalies, a => Assert.True(-a.Amount > 5 * largestRegularSpend));
    }

    [Fact]
    public void MoreThanTheMaximumMonths_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => SyntheticHistoryGenerator.Generate(From, From.AddMonths(SyntheticHistoryGenerator.MaxMonths), seed: 1));
    }
}
