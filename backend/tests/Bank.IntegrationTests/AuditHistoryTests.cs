using System.Net.Http.Json;
using Bank.Contracts.Finance;
using Bank.IntegrationTests.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Bank.IntegrationTests;

/// <summary>Every change to user-maintained finance data leaves its previous version behind (temporal tables).</summary>
[Collection(ApiCollection.Name)]
public sealed class AuditHistoryTests
{
    private readonly ApiFactory _factory;

    public AuditHistoryTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task UpdatingAndDeletingACard_KeepsEveryEarlierVersion()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var card = await CreditCardTests.CreateCardAsync(client, new CreditCardRequest("Bonus", 50_000m, "TRY", 20, 10));

        var update = new HttpRequestMessage(HttpMethod.Put, $"/api/finance/credit-cards/{card.Id}")
        {
            Content = JsonContent.Create(new CreditCardRequest("Bonus", 75_000m, "TRY", 20, 10))
        };
        update.Headers.TryAddWithoutValidation("If-Match", card.Version);
        (await client.SendAsync(update)).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/api/finance/credit-cards/{card.Id}")).EnsureSuccessStatusCode();

        await using var connection = new SqlConnection(ApiFactory.ConnectionString);
        var previous = (await connection.QueryAsync<(decimal Limit, bool Deleted)>(
            """
            SELECT LimitAmount, CAST(CASE WHEN DeletedAt IS NULL THEN 0 ELSE 1 END AS BIT)
            FROM finance.CreditCardsHistory
            WHERE CreditCardId = @Id
            ORDER BY ValidTo
            """, new { card.Id })).ToList();

        // Version 1 (50 000) was replaced by the update, version 2 (75 000) by the soft delete.
        Assert.Equal([(50_000m, false), (75_000m, false)], previous);
    }

    [Fact]
    public async Task PastStateCanBeQueriedAsOfAPointInTime()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var card = await CreditCardTests.CreateCardAsync(client, new CreditCardRequest("Bonus", 50_000m, "TRY", 20, 10));

        await using var connection = new SqlConnection(ApiFactory.ConnectionString);
        var beforeChange = await connection.ExecuteScalarAsync<DateTime>("SELECT SYSUTCDATETIME()");

        var update = new HttpRequestMessage(HttpMethod.Put, $"/api/finance/credit-cards/{card.Id}")
        {
            Content = JsonContent.Create(new CreditCardRequest("Bonus", 90_000m, "TRY", 20, 10))
        };
        update.Headers.TryAddWithoutValidation("If-Match", card.Version);
        (await client.SendAsync(update)).EnsureSuccessStatusCode();

        var limitThen = await connection.ExecuteScalarAsync<decimal>(
            "SELECT LimitAmount FROM finance.CreditCards FOR SYSTEM_TIME AS OF @At WHERE CreditCardId = @Id",
            new { At = beforeChange, card.Id });

        Assert.Equal(50_000m, limitThen);
    }
}
