using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Finance;
using Bank.IntegrationTests.Infrastructure;
using Microsoft.Data.SqlClient;

namespace Bank.IntegrationTests;

/// <remarks>The API clock is pinned to 31 March 2026 (see <see cref="ApiFactory.Now"/>), so projections start on 1 April.</remarks>
[Collection(ApiCollection.Name)]
public sealed class ProjectionTests
{
    private const string Url = "/api/finance/projection";

    private readonly ApiFactory _factory;

    public ProjectionTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Projection_CombinesAccountBalanceRecurringItemsAndCardStatements()
    {
        var (client, userId) = await _factory.CreateUserAsync();
        await AddTryAccountAsync(userId, balance: 5_000m);

        var card = await CreditCardTests.CreateCardAsync(client);
        await client.PostAsJsonAsync("/api/finance/recurring-cash-flows",
            new RecurringCashFlowRequest("Maaş", "Income", "Maaş", 45_000m, "TRY", "Monthly", 1, new(2026, 1, 15), null, null));
        await client.PostAsJsonAsync("/api/finance/recurring-cash-flows",
            new RecurringCashFlowRequest("Kira", "Expense", "Konut", 20_000m, "TRY", "Monthly", 1, new(2026, 1, 1), null, null));
        await client.PostAsJsonAsync("/api/finance/installment-plans",
            new InstallmentPlanRequest(card.Id, "Laptop", 1000m, 3, new(2026, 3, 25)));

        var projection = await client.GetFromJsonAsync<ProjectionResponse>($"{Url}?months=3");

        Assert.Equal(new DateOnly(2026, 4, 1), projection!.From);
        Assert.Equal(new DateOnly(2026, 6, 30), projection.To);
        Assert.Equal(5_000m, projection.OpeningBalance);

        var months = projection.Months;
        Assert.Equal([4, 5, 6], months.Select(m => m.Month));
        Assert.Equal([333.34m, 0m, 666.66m], months.Select(m => m.CardPayments));

        // Rent on the 1st, salary on the 15th: April closes positive but starts by going negative.
        var april = months[0];
        Assert.Equal(-15_000m, april.LowestBalance);
        Assert.Equal(new DateOnly(2026, 4, 1), april.LowestBalanceDate);
        Assert.True(april.BalanceGoesNegative);
        Assert.False(april.IsDeficit);
        Assert.Equal(5_000m + 45_000m - 20_000m - 333.34m, april.ClosingBalance);

        var statement = Assert.Single(projection.Entries, e => e.Kind == "CardPayment" && e.Date == new DateOnly(2026, 4, 30));
        Assert.Equal("Laptop (1/3)", Assert.Single(statement.Charges).Description);
    }

    [Fact]
    public async Task Projection_OnlyContainsTheCallersData()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var stranger = await _factory.CreateAuthenticatedClientAsync();
        await owner.PostAsJsonAsync("/api/finance/recurring-cash-flows",
            new RecurringCashFlowRequest("Maaş", "Income", "Maaş", 45_000m, "TRY", "Monthly", 1, new(2026, 1, 15), null, null));

        var projection = await stranger.GetFromJsonAsync<ProjectionResponse>(Url);

        Assert.Empty(projection!.Entries);
        Assert.All(projection.Months, m => Assert.Equal(0m, m.Income));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task Projection_HorizonOutOfRange_Returns400(int months)
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"{Url}?months={months}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // There is no account API yet; balances are seeded directly.
    private static async Task AddTryAccountAsync(long userId, decimal balance)
    {
        await using var connection = new SqlConnection(ApiFactory.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.Accounts (UserId, AccountType, CurrencyCode, Iban, Balance)
            VALUES (@UserId, 'Demand', 'TRY', LEFT(CONCAT('TR', REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '')), 26), @Balance)
            """;
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Balance", balance);
        await command.ExecuteNonQueryAsync();
    }
}
