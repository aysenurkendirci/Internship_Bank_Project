using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Auth;
using Bank.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Bank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class SecurityTests
{
    private readonly ApiFactory _factory;

    public SecurityTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_IsThrottledAfterTooManyAttempts()
    {
        // A separate host with a low limit, so the rest of the suite is not throttled.
        using var throttled = _factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:Auth:PermitLimit", "3"));
        var client = throttled.CreateClient();
        var attempt = new LoginRequest(TestUsers.NewTcKimlikNo(77_001), "wrong-password");

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
            statuses.Add((await client.PostAsJsonAsync("/api/auth/login", attempt)).StatusCode);

        var blocked = await client.PostAsJsonAsync("/api/auth/login", attempt);
        var problem = await blocked.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests], statuses);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.Contains("Retry-After"));
        Assert.Equal("Çok fazla deneme", problem!.Title);
    }

    [Fact]
    public async Task FinancialData_IsNeverCached_AndCarriesBaselineSecurityHeaders()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/finance/credit-cards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task ErrorResponses_CarryTheHeadersToo()
    {
        var response = await _factory.CreateClient().GetAsync("/api/finance/credit-cards");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
    }
}
