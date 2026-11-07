using System.Net.Http.Json;
using System.Text.Json;
using Bank.IntegrationTests.Infrastructure;

namespace Bank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class CorrelationIdTests
{
    private const string Header = "X-Correlation-Id";

    private readonly ApiFactory _factory;

    public CorrelationIdTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task EveryResponse_GetsAnId()
    {
        var response = await _factory.CreateClient().GetAsync("/health/live");

        Assert.Matches("^[0-9a-f]{32}$", response.Headers.GetValues(Header).Single());
    }

    [Fact]
    public async Task CallersId_IsKept()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(Header, "gateway-1234-abcd");

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal("gateway-1234-abcd", response.Headers.GetValues(Header).Single());
    }

    [Fact]
    public async Task SomethingThatIsNotAnId_IsReplaced()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.TryAddWithoutValidation(Header, "x\" injected=\"log line");

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Matches("^[0-9a-f]{32}$", response.Headers.GetValues(Header).Single());
    }

    [Fact]
    public async Task ErrorResponses_CarryTheSameId()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/finance/credit-cards/999999999");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(response.Headers.GetValues(Header).Single(), problem.GetProperty("correlationId").GetString());
    }
}
