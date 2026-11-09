using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Account;
using Bank.Contracts.Auth;
using Bank.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ProfileTests
{
    private readonly ApiFactory _factory;

    public ProfileTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Profile_ShowsTheCustomer_WithTheIdentityNumberMasked()
    {
        var registration = TestUsers.NewRegistration();
        var (client, _) = await _factory.CreateUserAsync(registration);

        var profile = await client.GetFromJsonAsync<ProfileResponse>("/api/me");

        Assert.Equal(registration.Email, profile!.Email);
        Assert.DoesNotContain(registration.TcNo, profile.MaskedTcNo);
        Assert.Contains("*", profile.MaskedTcNo);
    }

    [Fact]
    public async Task ContactDetails_CanBeChanged()
    {
        var (client, _) = await _factory.CreateUserAsync();
        var email = $"new-{Guid.NewGuid():N}@example.com";

        var response = await client.PutAsJsonAsync("/api/me", new UpdateProfileRequest(email, "5551112233"));
        var updated = await response.Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((email, "5551112233"), (updated!.Email, updated.Phone));
    }

    [Fact]
    public async Task AnEmailInUseByAnotherCustomer_Returns409()
    {
        var other = TestUsers.NewRegistration();
        await _factory.CreateUserAsync(other);
        var (client, _) = await _factory.CreateUserAsync();

        var response = await client.PutAsJsonAsync("/api/me", new UpdateProfileRequest(other.Email, "5551112233"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task WrongCurrentPassword_ChangesNothing()
    {
        var registration = TestUsers.NewRegistration();
        var (client, _) = await _factory.CreateUserAsync(registration);

        var response = await client.PutAsJsonAsync("/api/me/password", new ChangePasswordRequest("not-the-password", "NewP@ssw0rd"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(registration.TcNo, TestUsers.Password)).StatusCode);
    }

    [Fact]
    public async Task NewPasswordEqualToTheCurrentOne_Returns400()
    {
        var (client, _) = await _factory.CreateUserAsync();

        var response = await client.PutAsJsonAsync("/api/me/password", new ChangePasswordRequest(TestUsers.Password, TestUsers.Password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangingThePassword_EndsOtherSessions_AndKeepsThisOne()
    {
        var registration = TestUsers.NewRegistration();
        var (client, _) = await _factory.CreateUserAsync(registration);
        var otherDevice = RefreshCookie(await LoginAsync(registration.TcNo, TestUsers.Password));

        var change = await client.PutAsJsonAsync("/api/me/password", new ChangePasswordRequest(TestUsers.Password, "NewP@ssw0rd"));

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(otherDevice)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(RefreshCookie(change))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(registration.TcNo, TestUsers.Password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(registration.TcNo, "NewP@ssw0rd")).StatusCode);
    }

    private Task<HttpResponseMessage> LoginAsync(string tcNo, string password)
        => _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(tcNo, password));

    private Task<HttpResponseMessage> RefreshAsync(string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("X-Requested-By", "bank-web");
        request.Headers.Add("Cookie", cookie);
        return _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false }).SendAsync(request);
    }

    private static string RefreshCookie(HttpResponseMessage response)
        => response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("refresh_token=")).Split(';')[0];
}
