using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Auth;
using Bank.IntegrationTests.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class RefreshTokenTests
{
    private const string CookieName = "refresh_token";

    private readonly ApiFactory _factory;

    public RefreshTokenTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task SignIn_SetsAHardenedCookie_AndKeepsTheTokenOutOfTheBody()
    {
        var (response, _) = await RegisterAsync();

        var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(CookieName + "="));
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_ReturnsANewAccessTokenAndRotatesTheCookie()
    {
        var (_, first) = await RegisterAsync();

        var refreshed = await RefreshAsync(first);
        var body = await refreshed.Content.ReadFromJsonAsync<AuthResponse>();
        var second = CookieValue(refreshed);

        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.False(string.IsNullOrEmpty(body!.Token));
        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task TheNewAccessToken_Works()
    {
        var (_, first) = await RegisterAsync();
        var body = await (await RefreshAsync(first)).Content.ReadFromJsonAsync<AuthResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", body!.Token);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/accounts")).StatusCode);
    }

    // The scenario ADR-0004 names: a token that was already used means someone holds a copy.
    [Fact]
    public async Task UsingATokenTwice_RevokesEveryTokenOfTheUser()
    {
        var (_, first) = await RegisterAsync();
        var second = CookieValue(await RefreshAsync(first));

        var replay = await RefreshAsync(first);
        var legitimate = await RefreshAsync(second);

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, legitimate.StatusCode);
    }

    [Fact]
    public async Task OtherUsersSessions_AreNotAffectedByAReplay()
    {
        var (_, victimFirst) = await RegisterAsync();
        var (_, bystander) = await RegisterAsync();
        await RefreshAsync(victimFirst);

        await RefreshAsync(victimFirst);

        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(bystander)).StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheToken_SoACopyStopsWorking()
    {
        var (_, token) = await RegisterAsync();

        var logout = await SendAsync("/api/auth/logout", token);
        var afterwards = await RefreshAsync(token);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterwards.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutAToken_IsNotAnError()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync("/api/auth/logout", token: null)).StatusCode);
    }

    [Theory]
    [InlineData("/api/auth/refresh")]
    [InlineData("/api/auth/logout")]
    public async Task WithoutTheAppHeader_IsRefused_AndTheTokenStaysValid(string url)
    {
        var (_, token) = await RegisterAsync();

        var refused = await SendAsync(url, token, appHeader: false);

        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(token)).StatusCode);
    }

    [Fact]
    public async Task WithoutACookieOrWithGarbage_Returns401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(token: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync("not-a-real-token")).StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_Returns401()
    {
        var (_, token) = await RegisterAsync();
        await using var connection = new SqlConnection(ApiFactory.ConnectionString);
        // The API clock is pinned to 31 March 2026 (see ApiFactory.Now); this is a day earlier. The check
        // constraint wants the expiry after creation, so creation moves back too.
        await connection.ExecuteAsync(
            "UPDATE dbo.RefreshTokens SET CreatedAt = '2026-03-01', ExpiresAt = '2026-03-30' WHERE TokenHash = HASHBYTES('SHA2_256', @Token)",
            new { Token = Ascii(token) });

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(token)).StatusCode);
    }

    [Fact]
    public async Task OnlyAHashIsStored()
    {
        var (_, token) = await RegisterAsync();
        await using var connection = new SqlConnection(ApiFactory.ConnectionString);

        var stored = await connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM dbo.RefreshTokens WHERE TokenHash = HASHBYTES('SHA2_256', @Token)", new { Token = Ascii(token) });
        var plain = await connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM dbo.RefreshTokens WHERE CAST(TokenHash AS VARCHAR(MAX)) LIKE '%' + @Token + '%'", new { Token = Ascii(token) });

        Assert.Equal((1, 0), (stored, plain));
    }

    [Fact]
    public async Task ConcurrentRefreshesWithOneToken_LetExactlyOneThrough()
    {
        var (_, token) = await RegisterAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => RefreshAsync(token)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
    }

    private async Task<(HttpResponseMessage Response, string Token)> RegisterAsync()
    {
        var response = await Client().PostAsJsonAsync("/api/auth/register", TestUsers.NewRegistration());
        response.EnsureSuccessStatusCode();
        return (response, CookieValue(response));
    }

    private Task<HttpResponseMessage> RefreshAsync(string? token) => SendAsync("/api/auth/refresh", token);

    // Cookies are set with Secure and the test server speaks plain HTTP, so the client's cookie handling would
    // never send them back. The header is built by hand instead.
    private Task<HttpResponseMessage> SendAsync(string url, string? token, bool appHeader = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (appHeader)
            request.Headers.Add("X-Requested-By", "bank-web");
        if (token is not null)
            request.Headers.Add("Cookie", $"{CookieName}={token}");
        return Client().SendAsync(request);
    }

    // The API hashes the token as UTF-8 bytes. Dapper sends strings as NVARCHAR (UTF-16), which SQL Server would
    // hash differently; the token is plain ASCII, so VARCHAR gives the same bytes.
    private static DbString Ascii(string value) => new() { Value = value, IsAnsi = true };

    private HttpClient Client() => _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static string CookieValue(HttpResponseMessage response)
        => response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(CookieName + "="))
            .Split(';')[0][(CookieName.Length + 1)..];
}
