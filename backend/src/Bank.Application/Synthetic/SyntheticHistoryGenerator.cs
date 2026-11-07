namespace Bank.Application.Synthetic;

/// <param name="IsAnomaly">
/// True for the deliberately unusual purchases the generator injects. Anomaly detection can be
/// scored against these known answers.
/// </param>
public sealed record SyntheticTransaction(DateOnly Date, string Description, decimal Amount, string Category, bool IsAnomaly);

/// <summary>
/// Produces a realistic, reproducible transaction history for one salaried person in Istanbul:
/// fixed bills and subscriptions, seasonal heating, inflating everyday spending and the occasional
/// unusually large purchase. Real personal finance data is scarce in open datasets, so demos and
/// analytics are developed against this profile.
/// </summary>
/// <remarks>
/// The same seed always yields the same history. <see cref="Random"/> created with a seed keeps
/// its legacy algorithm in modern .NET precisely for this kind of reproducibility.
/// </remarks>
public static class SyntheticHistoryGenerator
{
    public const int MaxMonths = 36;

    // Everyday prices rise about 2.5% a month, roughly in line with recent Turkish inflation.
    private const double MonthlyInflation = 0.025;
    private const double AnomalyProbabilityPerMonth = 0.2;

    private static readonly string[] Groceries = ["MİGROS", "A101", "BİM", "ŞOK MARKET", "CARREFOURSA"];
    private static readonly string[] Restaurants = ["KAHVE DÜNYASI", "BURGER KING", "DÖNERCİ HASAN", "STARBUCKS", "SUSHICO"];
    private static readonly string[] Electronics = ["TEKNOSA", "MEDIAMARKT", "VATAN BİLGİSAYAR"];

    // Natural gas use by month (January first): heating dominates the winter bill.
    private static readonly double[] HeatingSeason = [2.6, 2.4, 1.8, 1.0, 0.5, 0.3, 0.3, 0.3, 0.4, 0.8, 1.6, 2.4];

    public static IReadOnlyList<SyntheticTransaction> Generate(DateOnly from, DateOnly to, int seed)
    {
        if (to < from)
            throw new ArgumentException("The end date must not be before the start date.", nameof(to));
        if ((to.Year - from.Year) * 12 + to.Month - from.Month + 1 > MaxMonths)
            throw new ArgumentException($"At most {MaxMonths} months can be generated.", nameof(to));

        var random = new Random(seed);
        var transactions = new List<SyntheticTransaction>();

        var monthIndex = 0;
        for (var month = new DateOnly(from.Year, from.Month, 1); month <= to; month = month.AddMonths(1), monthIndex++)
        {
            var priceLevel = Math.Pow(1 + MonthlyInflation, monthIndex);
            var builder = new MonthBuilder(month, random, priceLevel, transactions);

            builder.Fixed(1, "KİRA ÖDEMESİ", -20_000m, "Konut");
            builder.Fixed(3, "MACFIT SPOR SALONU", -1_200m, "Spor");
            builder.Fixed(5, "NETFLIX.COM", -229.99m, "Abonelik");
            builder.Fixed(12, "SPOTIFY", -99.99m, "Abonelik");
            builder.Fixed(18, "TÜRK TELEKOM İNTERNET", -450m, "Fatura");
            builder.Fixed(PreviousBusinessDay(month, 15), "MAAŞ ÖDEMESİ", 45_000m, "Maaş");

            builder.Variable(10, "ENERJİSA ELEKTRİK", 650, 0.20, "Fatura");
            builder.Variable(20, "İGDAŞ DOĞALGAZ", 1_000 * HeatingSeason[month.Month - 1], 0.15, "Fatura");
            builder.Variable(22, "İSKİ SU", 250, 0.15, "Fatura");

            builder.Repeating(Groceries, timesPerWeek: 2.5, min: 250, max: 1_400, "Market");
            builder.Repeating(Restaurants, timesPerWeek: 1.2, min: 150, max: 900, "Restoran");
            builder.Repeating(["İSTANBULKART DOLUM"], timesPerWeek: 1, min: 400, max: 400, "Ulaşım");

            if (random.NextDouble() < AnomalyProbabilityPerMonth)
                builder.Anomaly(Electronics, min: 12_000, max: 35_000, "Elektronik");
        }

        return transactions
            .Where(t => t.Date >= from && t.Date <= to)
            .OrderBy(t => t.Date)
            .ToList();
    }

    // Payroll runs on the previous working day when payday falls on a weekend.
    private static int PreviousBusinessDay(DateOnly month, int day)
    {
        var date = new DateOnly(month.Year, month.Month, day);
        return date.DayOfWeek switch
        {
            DayOfWeek.Saturday => day - 1,
            DayOfWeek.Sunday => day - 2,
            _ => day
        };
    }

    private sealed class MonthBuilder(DateOnly month, Random random, double priceLevel, List<SyntheticTransaction> sink)
    {
        private readonly int _daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);

        public void Fixed(int day, string description, decimal amount, string category)
            => Add(day, description, amount, category);

        /// <summary>A bill on a fixed day whose amount varies by ±<paramref name="spread"/> around an inflating base.</summary>
        public void Variable(int day, string description, double baseAmount, double spread, string category)
            => Add(day, description, -Cents(baseAmount * priceLevel * (1 + spread * (2 * random.NextDouble() - 1))), category);

        /// <summary>Spending that happens on random days, about <paramref name="timesPerWeek"/> times a week.</summary>
        public void Repeating(string[] merchants, double timesPerWeek, double min, double max, string category)
        {
            var count = (int)Math.Round(timesPerWeek * _daysInMonth / 7.0 * (0.8 + 0.4 * random.NextDouble()));
            for (var i = 0; i < count; i++)
                Add(
                    random.Next(1, _daysInMonth + 1),
                    merchants[random.Next(merchants.Length)],
                    -Cents((min + (max - min) * random.NextDouble()) * priceLevel),
                    category);
        }

        public void Anomaly(string[] merchants, double min, double max, string category)
            => Add(
                random.Next(1, _daysInMonth + 1),
                merchants[random.Next(merchants.Length)],
                -Cents(min + (max - min) * random.NextDouble()),
                category,
                isAnomaly: true);

        private void Add(int day, string description, decimal amount, string category, bool isAnomaly = false)
            => sink.Add(new SyntheticTransaction(new DateOnly(month.Year, month.Month, day), description, amount, category, isAnomaly));

        private static decimal Cents(double amount) => Math.Round((decimal)amount, 2, MidpointRounding.AwayFromZero);
    }
}
