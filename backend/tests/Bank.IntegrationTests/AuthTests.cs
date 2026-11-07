using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Auth;
using Bank.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Bank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthTests
{
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_ThenLogin_GivesAccessToProtectedEndpoints()
    {
        var client = _factory.CreateClient();
        var registration = TestUsers.NewRegistration();
        (await client.PostAsJsonAsync("/api/auth/register", registration)).EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(registration.TcNo, TestUsers.Password));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new("Bearer", auth!.Token);
        var dashboard = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/finance/credit-cards");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidPayload_Returns400WithFieldErrors()
    {
        var invalid = TestUsers.NewRegistration() with { TcNo = "12345678901", Membership = "Gold" };

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", invalid);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("TcNo", problem!.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Membership", problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_SameIdentityTwice_Returns409()
    {
        var client = _factory.CreateClient();
        var registration = TestUsers.NewRegistration();
        await client.PostAsJsonAsync("/api/auth/register", registration);

        var second = await client.PostAsJsonAsync("/api/auth/register", registration with { Email = "other@example.com" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUserAndWrongPassword_AreIndistinguishable()
    {
        var client = _factory.CreateClient();
        var registration = TestUsers.NewRegistration();
        await client.PostAsJsonAsync("/api/auth/register", registration);

        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(registration.TcNo, "wrong-password"));
        var unknownUser = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(TestUsers.NewTcKimlikNo(99_999), "x"));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        Assert.Equal(
            (await wrongPassword.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail,
            (await unknownUser.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail);
    }
}
