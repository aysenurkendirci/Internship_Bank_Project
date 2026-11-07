using System.Globalization;
using Bank.Application.Synthetic;
using Bank.Domain.Analytics;
using Bank.Domain.Common;

// Reproduces every figure in docs/research/EVALUATION.md.
// Usage: dotnet run --project tools/Bank.Evaluation -c Release
//
// Seeds 1–10 were used while developing the analytics; seeds 101–150 were held out and are only used here.

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var tuningSeeds = Enumerable.Range(1, 10).ToArray();
var heldOutSeeds = Enumerable.Range(101, 50).ToArray();

Console.WriteLine("# Evaluation results\n");
RecurringDetection("tuning seeds 1-10", tuningSeeds);
RecurringDetection("held-out seeds 101-150", heldOutSeeds);
AnomalyDetection("tuning seeds 1-10", tuningSeeds);
AnomalyDetection("held-out seeds 101-150", heldOutSeeds);
AnomalySensitivity(heldOutSeeds);
Forecasting(heldOutSeeds);

static List<HistoryEntry> ToHistory(IEnumerable<SyntheticTransaction> generated)
    => generated.Select(t => new HistoryEntry(t.Date, t.Description, t.Amount, t.Category)).ToList();

static IReadOnlyList<SyntheticTransaction> Year(int seed)
    => SyntheticHistoryGenerator.Generate(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), seed);

static void RecurringDetection(string label, int[] seeds)
{
    var truth = new[]
    {
        "KİRA ÖDEMESİ", "MACFIT SPOR SALONU", "NETFLIX.COM", "SPOTIFY", "TÜRK TELEKOM İNTERNET",
        "MAAŞ ÖDEMESİ", "ENERJİSA ELEKTRİK", "İGDAŞ DOĞALGAZ", "İSKİ SU"
    }.Select(TextFolding.Fold).ToHashSet();

    int tp = 0, fp = 0, fn = 0;
    foreach (var seed in seeds)
    {
        var found = RecurringPaymentDetector.Detect(ToHistory(Year(seed))).Select(p => TextFolding.Fold(p.Merchant)).ToHashSet();
        tp += found.Intersect(truth).Count();
        fp += found.Except(truth).Count();
        fn += truth.Except(found).Count();
    }

    Console.WriteLine($"## Recurring payment detection ({label})");
    Console.WriteLine($"TP={tp} FP={fp} FN={fn} precision={Ratio(tp, tp + fp)} recall={Ratio(tp, tp + fn)}\n");
}

static void AnomalyDetection(string label, int[] seeds)
{
    int tp = 0, fp = 0, fn = 0, normal = 0;
    foreach (var seed in seeds)
    {
        var generated = Year(seed);
        var history = ToHistory(generated);
        var labelled = history.Where((_, i) => generated[i].IsAnomaly).ToHashSet();
        var flagged = AnomalyDetector.Detect(history).Select(a => a.Entry).ToHashSet();

        tp += flagged.Intersect(labelled).Count();
        fp += flagged.Except(labelled).Count();
        fn += labelled.Except(flagged).Count();
        normal += history.Count(e => e.IsOutflow) - labelled.Count;
    }

    Console.WriteLine($"## Anomaly detection, labelled anomalies ({label})");
    Console.WriteLine($"TP={tp} FP={fp} FN={fn} precision={Ratio(tp, tp + fp)} recall={Ratio(tp, tp + fn)} normal outflows={normal}\n");
}

// Injects one grocery purchase of k × the grocery median per month into clean histories.
static void AnomalySensitivity(int[] seeds)
{
    double[] multiples = [2, 3, 4, 5, 6, 8];
    Console.WriteLine("## Anomaly detection sensitivity (held-out seeds, 12 injections per profile)");
    Console.WriteLine("| threshold | " + string.Join(" | ", multiples.Select(k => $"k={k}")) + " | false positives |");
    Console.WriteLine("|---|" + string.Concat(multiples.Select(_ => "---|")) + "---|");

    foreach (var threshold in new[] { 2.0, 2.5, 3.0, AnomalyDetector.Threshold })
    {
        var recalls = new List<string>();
        int falsePositives = 0, normal = 0;

        foreach (var k in multiples)
        {
            int injected = 0, caught = 0;
            foreach (var seed in seeds)
            {
                var history = ToHistory(Year(seed).Where(t => !t.IsAnomaly));
                var median = Statistics.Median(history.Where(e => e.Category == "Market").Select(e => (double)-e.Amount).ToList());
                var spikes = Enumerable.Range(1, 12)
                    .Select(m => new HistoryEntry(new DateOnly(2025, m, 28), "MİGROS", -Math.Round((decimal)(median * k), 2), "Market"))
                    .ToList();
                var flagged = AnomalyDetector.Detect(history.Concat(spikes).ToList(), threshold).Select(a => a.Entry).ToHashSet();

                injected += spikes.Count;
                caught += spikes.Count(flagged.Contains);
                if (k == multiples[0])
                {
                    normal += history.Count(e => e.IsOutflow);
                    falsePositives += history.Count(flagged.Contains);
                }
            }
            recalls.Add(Ratio(caught, injected));
        }

        Console.WriteLine($"| {threshold:0.0} | {string.Join(" | ", recalls)} | {falsePositives}/{normal} |");
    }
    Console.WriteLine();
}

static void Forecasting(int[] seeds)
{
    static IReadOnlyList<decimal> Series(int seed)
    {
        var history = ToHistory(SyntheticHistoryGenerator.Generate(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31), seed));
        return EverydaySpending.MonthlyTotals(history, new YearMonth(2024, 1), 24);
    }

    // Oracle: the true expected value of each month, approximated by averaging 300 independent profiles.
    const int oracleProfiles = 300;
    var oracle = new double[24];
    foreach (var seed in Enumerable.Range(1000, oracleProfiles))
    {
        var series = Series(seed);
        for (var i = 0; i < 24; i++)
            oracle[i] += (double)series[i] / oracleProfiles;
    }

    var results = new Dictionary<ForecastMethod, List<BacktestResult>>();
    var chosen = new Dictionary<ForecastMethod, int>();
    var oracleErrors = new List<double>();

    foreach (var seed in seeds)
    {
        var series = Series(seed);
        var forecast = SpendingForecaster.Forecast(series, new YearMonth(2025, 12), horizon: 1);
        chosen[forecast.Method] = chosen.GetValueOrDefault(forecast.Method) + 1;
        foreach (var backtest in forecast.Backtests)
            (results.TryGetValue(backtest.Method, out var list) ? list : results[backtest.Method] = []).Add(backtest);
        for (var t = SpendingForecaster.MinimumHistory; t < 24; t++)
            oracleErrors.Add(Math.Abs((double)series[t] - oracle[t]) / (double)series[t]);
    }

    Console.WriteLine("## Forecasting everyday spending (held-out seeds, 24 months, one-step backtest of months 7-24)");
    Console.WriteLine("| method | MAPE (mean ± sd across profiles) | 95% interval coverage | chosen by backtest |");
    Console.WriteLine("|---|---|---|---|");
    foreach (var (method, list) in results)
    {
        var mapes = list.Select(b => b.MeanAbsolutePercentageError).ToList();
        Console.WriteLine($"| {method} | {mapes.Average():P1} ± {Statistics.StandardDeviation(mapes):P1} | {list.Average(b => b.IntervalCoverage):P0} | {chosen.GetValueOrDefault(method)}/{seeds.Length} |");
    }

    var selected = seeds.Select(seed =>
    {
        var f = SpendingForecaster.Forecast(Series(seed), new YearMonth(2025, 12), horizon: 1);
        return f.Backtests.Single(b => b.Method == f.Method);
    }).ToList();
    Console.WriteLine($"| Selected per profile | {selected.Average(b => b.MeanAbsolutePercentageError):P1} | {selected.Average(b => b.IntervalCoverage):P0} | |");
    Console.WriteLine($"| Oracle (true expected value) | {oracleErrors.Average():P1} | | |\n");
}

static string Ratio(int numerator, int denominator)
    => denominator == 0 ? "n/a" : ((double)numerator / denominator).ToString("0.000");
