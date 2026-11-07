using System.Net;
using Bank.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace Bank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class HealthTests
{
    private readonly ApiFactory _factory;

    public HealthTests(ApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_ReportHealthy_WithoutAuthentication(string url)
    {
        var response = await _factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Readiness_FailsWhenTheDatabaseIsUnreachable_WhileLivenessStaysUp()
    {
        // Nothing listens on port 1, so the connection is refused right away.
        using var broken = _factory.WithWebHostBuilder(b => b.UseSetting(
            "ConnectionStrings:BankDb", "Server=127.0.0.1,1;Database=Nowhere;User Id=x;Password=x;Connect Timeout=2;TrustServerCertificate=True"));
        var client = broken.CreateClient();

        var ready = await client.GetAsync("/health/ready");
        var live = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal("Unhealthy", await ready.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }
}
