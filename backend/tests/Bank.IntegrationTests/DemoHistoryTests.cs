using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Finance;
using Bank.IntegrationTests.Infrastructure;

namespace Bank.IntegrationTests;

/// <remarks>The API clock is pinned to 31 March 2026, so generated history ends on 30 March.</remarks>
[Collection(ApiCollection.Name)]
public sealed class DemoHistoryTests
{
    private const string Url = "/api/finance/transactions/demo";

    private readonly ApiFactory _factory;

    public DemoHistoryTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Demo_FillsTheHistoryUpToYesterday()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var result = await PostDemoAsync(client, months: 3);
        var page = await client.GetFromJsonAsync<PagedResponse<HistoryTransactionResponse>>("/api/finance/transactions?pageSize=200");

        Assert.True(result.ImportedRows > 50);
        Assert.Equal(result.ImportedRows, page!.TotalCount);
        // Three calendar months ending yesterday; rent is always paid on the 1st.
        Assert.Equal(new DateOnly(2026, 3, 30), page.Items.Max(t => t.Date));
        Assert.Equal(new DateOnly(2026, 1, 1), page.Items.Min(t => t.Date));
    }

    [Fact]
    public async Task Demo_WithTheSameSeedTwice_AddsNothingTheSecondTime()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var first = await PostDemoAsync(client, months: 2);
        var second = await PostDemoAsync(client, months: 2);

        Assert.Equal(0, second.ImportedRows);
        Assert.Equal(first.TotalRows, second.DuplicateRows);
    }

    [Fact]
    public async Task Demo_OutOfRangeMonths_Returns400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync($"{Url}?months=25", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<ImportResultResponse> PostDemoAsync(HttpClient client, int months)
    {
        var response = await client.PostAsync($"{Url}?months={months}&seed=42", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ImportResultResponse>())!;
    }
}
