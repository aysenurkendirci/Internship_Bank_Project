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
    public async Task SessionRefresh_DoesNotCountAgainstTheLoginLimit()
    {
        using var throttled = _factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:Auth:PermitLimit", "3"));
        var client = throttled.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-By", "bank-web");

        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/auth/refresh", null)).StatusCode);
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
    public async Task AccessToken_CarriesNoPersonalData()
    {
        var registration = TestUsers.NewRegistration();
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", registration);
        var token = (await response.Content.ReadFromJsonAsync<AuthResponse>())!.Token;

        // A JWT is only signed: its payload is plain base64url that anyone holding the token can read.
        var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=')));

        Assert.DoesNotContain(registration.Email, json);
        Assert.DoesNotContain(registration.TcNo, json);
        Assert.DoesNotContain(registration.FirstName, json);
    }

    [Fact]
    public async Task ErrorResponses_CarryTheHeadersToo()
    {
        var response = await _factory.CreateClient().GetAsync("/api/finance/credit-cards");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
    }
}
