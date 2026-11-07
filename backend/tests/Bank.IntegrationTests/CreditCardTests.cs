using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Finance;
using Bank.IntegrationTests.Infrastructure;

namespace Bank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class CreditCardTests
{
    private const string Url = "/api/finance/credit-cards";

    private static readonly CreditCardRequest Bonus = new("Bonus", 50_000m, "TRY", StatementDay: 20, PaymentDueDays: 10);

    private readonly ApiFactory _factory;

    public CreditCardTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_ReturnsCreatedCardWithVersion()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();

        var response = await owner.PostAsJsonAsync(Url, Bonus);
        var card = await response.Content.ReadFromJsonAsync<CreditCardResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"{Url}/{card!.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(50_000m, card.LimitAmount);
        Assert.False(string.IsNullOrEmpty(card.Version));
    }

    [Fact]
    public async Task OtherUser_CannotSeeChangeOrDeleteTheCard()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var stranger = await _factory.CreateAuthenticatedClientAsync();
        var card = await CreateCardAsync(owner);

        var list = await stranger.GetFromJsonAsync<List<CreditCardResponse>>(Url);
        var get = await stranger.GetAsync($"{Url}/{card.Id}");
        var put = await stranger.SendAsync(Put(card.Id, Bonus with { LimitAmount = 1m }, card.Version));
        var delete = await stranger.DeleteAsync($"{Url}/{card.Id}");

        Assert.Empty(list!);
        // 404 rather than 403: answering "forbidden" would confirm that the card exists.
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        var unchanged = await owner.GetFromJsonAsync<CreditCardResponse>($"{Url}/{card.Id}");
        Assert.Equal(50_000m, unchanged!.LimitAmount);
    }

    [Fact]
    public async Task Update_WithCurrentVersion_SucceedsAndIssuesNewVersion()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var card = await CreateCardAsync(owner);

        var response = await owner.SendAsync(Put(card.Id, Bonus with { LimitAmount = 75_000m }, card.Version));
        var updated = await response.Content.ReadFromJsonAsync<CreditCardResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(75_000m, updated!.LimitAmount);
        Assert.NotEqual(card.Version, updated.Version);
    }

    [Fact]
    public async Task Update_WithStaleVersion_Returns409AndKeepsNewerData()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var card = await CreateCardAsync(owner);

        // Two tabs opened the same card; the first one saves.
        await owner.SendAsync(Put(card.Id, Bonus with { LimitAmount = 75_000m }, card.Version));
        var second = await owner.SendAsync(Put(card.Id, Bonus with { StatementDay = 5 }, card.Version));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var current = await owner.GetFromJsonAsync<CreditCardResponse>($"{Url}/{card.Id}");
        Assert.Equal(75_000m, current!.LimitAmount);
        Assert.Equal(20, current.StatementDay);
    }

    [Fact]
    public async Task Update_WithoutIfMatch_Returns428()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var card = await CreateCardAsync(owner);

        var response = await owner.PutAsJsonAsync($"{Url}/{card.Id}", Bonus);

        Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidValues_Returns400()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();

        var response = await owner.PostAsJsonAsync(Url, new CreditCardRequest("", 0m, "GBP", 40, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_IsSoftAndHidesTheCard()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var card = await CreateCardAsync(owner);

        var delete = await owner.DeleteAsync($"{Url}/{card.Id}");
        var get = await owner.GetAsync($"{Url}/{card.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    internal static async Task<CreditCardResponse> CreateCardAsync(HttpClient client, CreditCardRequest? request = null)
    {
        var response = await client.PostAsJsonAsync(Url, request ?? Bonus);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreditCardResponse>())!;
    }

    private static HttpRequestMessage Put(long id, CreditCardRequest body, string version)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"{Url}/{id}") { Content = JsonContent.Create(body) };
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        return request;
    }
}
