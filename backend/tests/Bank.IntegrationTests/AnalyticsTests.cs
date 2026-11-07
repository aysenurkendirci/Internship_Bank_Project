using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Finance;
using Bank.IntegrationTests.Infrastructure;

namespace Bank.IntegrationTests;

/// <remarks>
/// The API clock is pinned to 31 March 2026. A 12-month demo history therefore runs from April 2025 to
/// 30 March 2026, and the last complete month is February 2026.
/// </remarks>
[Collection(ApiCollection.Name)]
public sealed class AnalyticsTests
{
    private const string Url = "/api/finance/analytics";

    private readonly ApiFactory _factory;

    public AnalyticsTests(ApiFactory factory) => _factory = factory;

    private async Task<HttpClient> UserWithDemoHistoryAsync(int months = 12)
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        (await client.PostAsync($"/api/finance/transactions/demo?months={months}&seed=42", null)).EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task CategoryTrends_CoverTheLastCompleteMonths()
    {
        var client = await UserWithDemoHistoryAsync();

        var trends = await client.GetFromJsonAsync<CategoryTrendsResponse>($"{Url}/category-trends?months=6");

        Assert.Equal(["2025-09", "2025-10", "2025-11", "2025-12", "2026-01", "2026-02"], trends!.Months);
        Assert.Equal("Konut", trends.Categories[0].Category);
        Assert.Contains(trends.Categories, c => c.Category == "Market");
        Assert.All(trends.Categories, c => Assert.Equal(6, c.MonthlySpending.Count));
    }

    [Fact]
    public async Task RecurringPayments_AreFoundAndMarkedWhenAlreadyTracked()
    {
        var client = await UserWithDemoHistoryAsync();
        await client.PostAsJsonAsync("/api/finance/recurring-cash-flows",
            new RecurringCashFlowRequest("Netflix", "Subscription", "Eğlence", 229.99m, "TRY", "Monthly", 1, new(2025, 4, 5), null, null));

        var payments = (await client.GetFromJsonAsync<List<RecurringPaymentResponse>>($"{Url}/recurring-payments"))!;

        var netflix = payments.Single(p => p.Merchant == "NETFLIX.COM");
        Assert.Equal(("Fixed", 229.99m, true), (netflix.Pattern, netflix.TypicalAmount, netflix.IsTracked));
        Assert.Equal("Variable", payments.Single(p => p.Merchant == "İGDAŞ DOĞALGAZ").Pattern);
        Assert.Equal("Inbound", payments.Single(p => p.Merchant == "MAAŞ ÖDEMESİ").Direction);
        Assert.False(payments.Single(p => p.Merchant == "SPOTIFY").IsTracked);
    }

    [Fact]
    public async Task Anomalies_AreOnlyTheInjectedOneOffPurchases()
    {
        var client = await UserWithDemoHistoryAsync();

        var anomalies = await client.GetFromJsonAsync<List<AnomalyResponse>>($"{Url}/anomalies?months=12");

        // The demo profile labels large electronics purchases as its anomalies; nothing else may be flagged.
        Assert.NotEmpty(anomalies!);
        Assert.All(anomalies!, a =>
        {
            Assert.Equal("Elektronik", a.Category);
            Assert.True(a.TimesTypical > 5);
        });
    }

    [Fact]
    public async Task Forecast_UsesTheCompleteMonthsAndReportsItsBacktest()
    {
        var client = await UserWithDemoHistoryAsync();

        var forecast = await client.GetFromJsonAsync<SpendingForecastResponse>($"{Url}/forecast?months=3");

        Assert.Equal(11, forecast!.BasedOnMonths);
        Assert.Equal(["2026-03", "2026-04", "2026-05"], forecast.Months.Select(m => m.Month));
        Assert.All(forecast.Months, m => Assert.True(m.Lower <= m.Expected && m.Expected <= m.Upper));
        Assert.Equal(3, forecast.Backtests.Count);
        Assert.Contains(forecast.Backtests, b => b.Method == forecast.Method);
    }

    [Fact]
    public async Task Forecast_WithTooLittleHistory_Returns400()
    {
        var client = await UserWithDemoHistoryAsync(months: 3);

        var response = await client.GetAsync($"{Url}/forecast");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Analytics_SeeOnlyTheCallersHistory()
    {
        await UserWithDemoHistoryAsync();
        var stranger = await _factory.CreateAuthenticatedClientAsync();

        var payments = await stranger.GetFromJsonAsync<List<RecurringPaymentResponse>>($"{Url}/recurring-payments");
        var trends = await stranger.GetFromJsonAsync<CategoryTrendsResponse>($"{Url}/category-trends");

        Assert.Empty(payments!);
        Assert.Empty(trends!.Categories);
    }
}
