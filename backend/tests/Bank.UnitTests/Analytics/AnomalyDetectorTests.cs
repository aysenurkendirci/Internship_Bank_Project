using Bank.Application.Synthetic;
using Bank.Domain.Analytics;

namespace Bank.UnitTests.Analytics;

public sealed class AnomalyDetectorTests
{
    private static readonly decimal[] GroceryAmounts =
        [420m, 510m, 380m, 650m, 470m, 560m, 390m, 610m, 440m, 530m, 500m, 455m];

    private static HistoryEntry Spend(int month, int day, string description, decimal amount, string category)
        => new(new DateOnly(2026, month, day), description, -amount, category);

    private static List<HistoryEntry> TypicalGroceries()
        => GroceryAmounts.Select((a, i) => Spend(i % 6 + 1, i + 2, "MİGROS", a, "Market")).ToList();

    [Fact]
    public void UnusuallyLargePurchaseInAWellKnownCategory_IsFlaggedAgainstThatCategory()
    {
        var history = TypicalGroceries();
        history.Add(Spend(6, 28, "MİGROS", 6_000m, "Market"));

        var anomaly = Assert.Single(AnomalyDetector.Detect(history));

        Assert.Equal(-6_000m, anomaly.Entry.Amount);
        Assert.Equal("Market", anomaly.ComparedWith);
        // The baseline includes the purchase itself (13 values); the median is robust to it: 500, the 7th value.
        Assert.Equal(500m, anomaly.TypicalAmount);
        Assert.Equal(12.0m, anomaly.TimesTypical);
    }

    [Fact]
    public void PurchaseInARareCategory_IsComparedWithAllSpending()
    {
        var history = TypicalGroceries();
        history.Add(Spend(4, 9, "TEKNOSA", 25_000m, "Elektronik"));

        var anomaly = Assert.Single(AnomalyDetector.Detect(history));

        Assert.Equal(AnomalyDetector.AllSpending, anomaly.ComparedWith);
    }

    [Fact]
    public void NormalVariation_IsNotFlagged()
    {
        var history = TypicalGroceries();
        history.Add(Spend(6, 28, "MİGROS", 900m, "Market"));

        Assert.Empty(AnomalyDetector.Detect(history));
    }

    [Fact]
    public void LargeRecurringPayment_IsExpected_NotAnAnomaly()
    {
        var history = TypicalGroceries();
        history.AddRange(Enumerable.Range(1, 6).Select(m => Spend(m, 1, "KİRA ÖDEMESİ", 20_000m, "Konut")));

        Assert.Empty(AnomalyDetector.Detect(history));
    }

    [Fact]
    public void UnusuallySmallPurchase_IsNotFlagged()
    {
        var history = TypicalGroceries();
        history.Add(Spend(6, 28, "MİGROS", 5m, "Market"));

        Assert.Empty(AnomalyDetector.Detect(history));
    }

    [Fact]
    public void IdenticalTypicalAmounts_StillLetAnOutlierThrough()
    {
        // More than half the values are equal, so the MAD is zero; the fallback must still work.
        var history = Enumerable.Range(1, 12).Select(i => Spend(i % 6 + 1, i + 2, "İSTANBULKART DOLUM", 400m, "Ulaşım")).ToList();
        history.Add(Spend(3, 25, "İSTANBULKART DOLUM", 350m, "Ulaşım"));
        history.Add(Spend(5, 26, "İSTANBULKART DOLUM", 4_000m, "Ulaşım"));

        var anomaly = Assert.Single(AnomalyDetector.Detect(history));
        Assert.Equal(-4_000m, anomaly.Entry.Amount);
    }

    [Fact]
    public void TooLittleHistory_ProducesNoVerdicts()
    {
        var history = TypicalGroceries().Take(AnomalyDetector.MinimumGroupSize - 2).ToList();
        history.Add(Spend(6, 28, "TEKNOSA", 25_000m, "Elektronik"));

        Assert.Empty(AnomalyDetector.Detect(history));
    }

    /// <summary>
    /// Evaluation on held-out synthetic profiles (seeds 101–110) against the generator's labelled anomalies.
    /// The threshold is the literature default and was not fitted; the ≥ 0.95 bar was fixed in advance.
    /// </summary>
    [Fact]
    public void HeldOutSyntheticProfiles_PrecisionAndRecallAreAtLeast95Percent()
    {
        int truePositives = 0, falsePositives = 0, falseNegatives = 0;
        foreach (var seed in Enumerable.Range(101, 10))
        {
            var generated = SyntheticHistoryGenerator.Generate(new(2025, 1, 1), new(2025, 12, 31), seed);
            var history = generated.Select(t => new HistoryEntry(t.Date, t.Description, t.Amount, t.Category)).ToList();
            var labelled = history.Where((_, i) => generated[i].IsAnomaly).ToHashSet();
            var flagged = AnomalyDetector.Detect(history).Select(a => a.Entry).ToHashSet();

            truePositives += flagged.Intersect(labelled).Count();
            falsePositives += flagged.Except(labelled).Count();
            falseNegatives += labelled.Except(flagged).Count();
        }

        var precision = (double)truePositives / (truePositives + falsePositives);
        var recall = (double)truePositives / (truePositives + falseNegatives);

        Assert.True(precision >= 0.95, $"precision {precision:0.000}");
        Assert.True(recall >= 0.95, $"recall {recall:0.000}");
    }
}
