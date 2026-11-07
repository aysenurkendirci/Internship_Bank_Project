using Bank.Application.Synthetic;
using Bank.Domain.Analytics;
using Bank.Domain.Common;

namespace Bank.UnitTests.Analytics;

public sealed class SpendingForecasterTests
{
    private static readonly YearMonth December = new(2025, 12);

    [Fact]
    public void PerfectLinearGrowth_IsForecastExactlyByTheTrendMethod()
    {
        decimal[] series = [1_000m, 1_100m, 1_200m, 1_300m, 1_400m, 1_500m, 1_600m, 1_700m];

        var forecast = SpendingForecaster.Forecast(series, December, horizon: 2);

        Assert.Equal(ForecastMethod.LinearTrend, forecast.Method);
        Assert.Equal([1_800m, 1_900m], forecast.Months.Select(m => m.Expected));
        Assert.Equal(0, forecast.Backtests.Single(b => b.Method == ForecastMethod.LinearTrend).MeanAbsolutePercentageError);
    }

    [Fact]
    public void Methods_ProduceTheirTextbookForecasts()
    {
        decimal[] series = [100m, 100m, 100m, 100m, 130m, 160m, 190m];

        Assert.Equal(190m, Single(series, ForecastMethod.Naive));
        Assert.Equal(160m, Single(series, ForecastMethod.MovingAverage));
        // Least-squares line through the last six values (100,100,100,130,160,190):
        // slope = 330 / 17.5 ≈ 18.857, intercept = 130 − 2.5 × slope ≈ 82.857, at x = 6 → 196.
        Assert.Equal(196m, Single(series, ForecastMethod.LinearTrend));
    }

    [Fact]
    public void ForecastMonths_FollowTheLastObservedMonth()
    {
        var forecast = SpendingForecaster.Forecast(Flat(8, 500m), December, horizon: 3);

        Assert.Equal([new YearMonth(2026, 1), new YearMonth(2026, 2), new YearMonth(2026, 3)], forecast.Months.Select(m => m.Month));
    }

    [Fact]
    public void Interval_WidensFurtherAhead_AndNeverGoesBelowZero()
    {
        decimal[] noisy = [900m, 1_300m, 800m, 1_400m, 700m, 1_200m, 1_000m, 1_500m, 600m, 1_100m];

        var months = SpendingForecaster.Forecast(noisy, December, horizon: 6, ForecastMethod.Naive).Months;

        var widths = months.Select(m => m.Upper - m.Lower).ToList();
        Assert.Equal(widths.Order(), widths);
        Assert.All(months, m => Assert.True(m.Lower >= 0));
    }

    [Fact]
    public void ConstantSpending_HasNoErrorAndNoUncertainty()
    {
        var forecast = SpendingForecaster.Forecast(Flat(8, 500m), December, horizon: 1);

        var month = Assert.Single(forecast.Months);
        Assert.Equal((500m, 500m, 500m), (month.Lower, month.Expected, month.Upper));
    }

    [Fact]
    public void TooShortHistory_IsRejected()
    {
        Assert.Throws<DomainException>(() => SpendingForecaster.Forecast(Flat(SpendingForecaster.MinimumHistory - 1, 500m), December, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void HorizonOutOfRange_IsRejected(int horizon)
    {
        Assert.Throws<DomainException>(() => SpendingForecaster.Forecast(Flat(8, 500m), December, horizon));
    }

    [Fact]
    public void EverydaySpending_LeavesOutRecurringPaymentsAndOneOffs()
    {
        var history = new List<HistoryEntry>();
        for (var month = 1; month <= 6; month++)
        {
            history.Add(new(new(2025, month, 1), "KİRA ÖDEMESİ", -20_000m, "Konut"));
            for (var day = 3; day <= 27; day += 4)
                history.Add(new(new(2025, month, day), "MİGROS", -(400m + day), "Market"));
        }
        history.Add(new(new(2025, 4, 18), "TEKNOSA", -30_000m, "Elektronik"));

        var totals = EverydaySpending.MonthlyTotals(history, new YearMonth(2025, 1), 6);

        // Only the seven grocery trips a month remain: 7 × 400 + (3 + 7 + ... + 27).
        Assert.All(totals, t => Assert.Equal(2_905m, t));
    }

    /// <summary>
    /// Calibration on held-out synthetic profiles: the 95% interval should contain the actual month in at
    /// least 80% of one-step backtests. This bar was set before measuring. (The accompanying point-error
    /// bar of MAPE ≤ 10% was not met and is discussed in the evaluation report rather than hidden here.)
    /// </summary>
    [Fact]
    public void HeldOutSyntheticProfiles_IntervalCoversAtLeast80Percent()
    {
        var coverages = Enumerable.Range(101, 10).Select(seed =>
        {
            var history = SyntheticHistoryGenerator.Generate(new(2024, 1, 1), new(2025, 12, 31), seed)
                .Select(t => new HistoryEntry(t.Date, t.Description, t.Amount, t.Category))
                .ToList();
            var series = EverydaySpending.MonthlyTotals(history, new YearMonth(2024, 1), 24);
            var forecast = SpendingForecaster.Forecast(series, December, horizon: 1);
            return forecast.Backtests.Single(b => b.Method == forecast.Method).IntervalCoverage;
        });

        Assert.True(coverages.Average() >= 0.80, $"coverage {coverages.Average():P0}");
    }

    private static decimal Single(decimal[] series, ForecastMethod method)
        => SpendingForecaster.Forecast(series, December, 1, method).Months[0].Expected;

    private static decimal[] Flat(int months, decimal value) => Enumerable.Repeat(value, months).ToArray();
}
