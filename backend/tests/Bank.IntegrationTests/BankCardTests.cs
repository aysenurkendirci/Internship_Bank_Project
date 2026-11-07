using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Cards;
using Bank.Contracts.Dashboard;
using Bank.IntegrationTests.Infrastructure;

namespace Bank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class BankCardTests
{
    private readonly ApiFactory _factory;

    public BankCardTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Settings_AreSaved_AndShownWithTheCard()
    {
        var (client, cardId) = await UserWithCardAsync();

        var response = await client.PutAsJsonAsync($"/api/cards/{cardId}/settings", new CardSettingsRequest(false, true, 2_500m, 20_000m));
        var card = (await client.GetFromJsonAsync<List<CardItem>>("/api/cards"))!.Single(c => c.CardId == cardId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((false, true), (card.Settings!.Contactless, card.Settings.OnlineUse));
        Assert.Equal((2_500m, 20_000m), (card.Limits!.DailyLimit, card.Limits.MonthlyLimit));
    }

    [Fact]
    public async Task MonthlyLimitBelowDaily_Returns400()
    {
        var (client, cardId) = await UserWithCardAsync();

        var response = await client.PutAsJsonAsync($"/api/cards/{cardId}/settings", new CardSettingsRequest(true, true, 10_000m, 5_000m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Card_CanBeBlockedAndOpenedAgain()
    {
        var (client, cardId) = await UserWithCardAsync();

        var blocked = await (await client.PostAsync($"/api/cards/{cardId}/block", null)).Content.ReadFromJsonAsync<CardSettingsResponse>();
        var opened = await (await client.PostAsync($"/api/cards/{cardId}/unblock", null)).Content.ReadFromJsonAsync<CardSettingsResponse>();

        Assert.Equal("Blocked", blocked!.Status);
        Assert.Equal("Active", opened!.Status);
    }

    [Fact]
    public async Task CancelledCard_CannotBeReopened()
    {
        var (client, cardId) = await UserWithCardAsync(status: "Cancelled");

        var response = await client.PostAsync($"/api/cards/{cardId}/unblock", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SomeoneElsesCard_LooksMissing()
    {
        var (_, cardId) = await UserWithCardAsync();
        var stranger = await _factory.CreateAuthenticatedClientAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync($"/api/cards/{cardId}/block", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await stranger.PutAsJsonAsync($"/api/cards/{cardId}/settings", new CardSettingsRequest(true, true, 0m, 0m))).StatusCode);
    }

    private async Task<(HttpClient Client, long CardId)> UserWithCardAsync(string status = "Active")
    {
        var (client, userId) = await _factory.CreateUserAsync();
        var (accountId, _) = await TestData.AddTryAccountAsync(userId, 0m);
        return (client, await TestData.AddCardAsync(userId, accountId, status));
    }
}
