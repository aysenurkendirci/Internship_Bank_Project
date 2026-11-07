using Bank.Domain.Analytics;
using Bank.Domain.Common;

namespace Bank.UnitTests.Analytics;

public sealed class CategoryTrendAnalyzerTests
{
    private static readonly YearMonth January = new(2026, 1);

    private static HistoryEntry Spend(int month, decimal amount, string? category, int day = 10)
        => new(new DateOnly(2026, month, day), "X", -amount, category);

    [Fact]
    public void SpendingIsTotalledPerCategoryAndMonth()
    {
        var report = CategoryTrendAnalyzer.Analyze(
        [
            Spend(1, 1_000m, "Market"), Spend(1, 500m, "Market"), Spend(2, 1_200m, "Market"),
            Spend(1, 300m, "Restoran"), Spend(3, 450m, "Restoran")
        ], January, 3);

        var market = report.Categories.Single(c => c.Category == "Market");
        var restaurant = report.Categories.Single(c => c.Category == "Restoran");

        Assert.Equal([1_500m, 1_200m, 0m], market.MonthlySpending);
        Assert.Equal([300m, 0m, 450m], restaurant.MonthlySpending);
        Assert.Equal([1_800m, 1_200m, 450m], report.MonthlyTotals);
    }

    [Fact]
    public void Categories_AreOrderedByTotal_WithTheirShare()
    {
        var report = CategoryTrendAnalyzer.Analyze(
            [Spend(1, 750m, "Restoran"), Spend(1, 250m, "Market")], January, 1);

        Assert.Equal(["Restoran", "Market"], report.Categories.Select(c => c.Category));
        Assert.Equal([0.75m, 0.25m], report.Categories.Select(c => c.Share));
    }

    [Fact]
    public void IncomeAndEntriesOutsideThePeriod_AreIgnored()
    {
        var report = CategoryTrendAnalyzer.Analyze(
        [
            new HistoryEntry(new(2026, 1, 15), "Maaş", 45_000m, "Maaş"),
            Spend(1, 100m, "Market"),
            new HistoryEntry(new(2025, 12, 31), "X", -999m, "Market"),
            new HistoryEntry(new(2026, 2, 1), "X", -999m, "Market")
        ], January, 1);

        var market = Assert.Single(report.Categories);
        Assert.Equal(100m, market.Total);
    }

    [Fact]
    public void MissingCategory_IsGroupedAsUncategorised()
    {
        var report = CategoryTrendAnalyzer.Analyze([Spend(1, 100m, null), Spend(1, 50m, " ")], January, 1);

        Assert.Equal(HistoryEntry.Uncategorised, Assert.Single(report.Categories).Category);
    }

    [Fact]
    public void ChangeAndSlope_DescribeTheDirection()
    {
        var report = CategoryTrendAnalyzer.Analyze(
            [Spend(1, 1_000m, "Market"), Spend(2, 1_100m, "Market"), Spend(3, 1_320m, "Market")], January, 3);

        var market = Assert.Single(report.Categories);
        Assert.Equal(0.2m, market.ChangeFromPreviousMonth);
        Assert.Equal(160m, market.SlopePerMonth);
    }

    [Fact]
    public void Change_IsUndefinedWhenThePreviousMonthWasZero()
    {
        var report = CategoryTrendAnalyzer.Analyze([Spend(2, 100m, "Market")], January, 2);

        Assert.Null(Assert.Single(report.Categories).ChangeFromPreviousMonth);
    }
}
