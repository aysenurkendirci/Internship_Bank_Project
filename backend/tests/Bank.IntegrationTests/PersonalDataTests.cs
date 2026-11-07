using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Bank.Contracts.Account;
using Bank.Contracts.Auth;
using Bank.IntegrationTests.Infrastructure;

namespace Bank.IntegrationTests;

/// <summary>KVKK right of access: a user can download everything stored about them, and only that.</summary>
[Collection(ApiCollection.Name)]
public sealed class PersonalDataTests
{
    private const string Url = "/api/me/data-export";

    private readonly ApiFactory _factory;

    public PersonalDataTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Export_ContainsTheUsersOwnData_AsADownload()
    {
        var registration = TestUsers.NewRegistration();
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/auth/register", registration)).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        await CreditCardTests.CreateCardAsync(client);
        var csv = new ByteArrayContent(Encoding.UTF8.GetBytes("Tarih;Açıklama;Tutar\n05.03.2026;KİRA;-20.000,00\n"));
        (await client.PostAsync("/api/finance/transactions/import", new MultipartFormDataContent { { csv, "file", "e.csv" } })).EnsureSuccessStatusCode();

        var response = await client.GetAsync(Url);
        var export = await response.Content.ReadFromJsonAsync<PersonalDataExport>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal((registration.TcNo, registration.Email), (export!.Profile.TcNo, export.Profile.Email));
        Assert.Single(export.CreditCards);
        Assert.Equal("KİRA", Assert.Single(export.Transactions).Description);
    }

    [Fact]
    public async Task Export_NeverIncludesAnotherUsersData()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        await CreditCardTests.CreateCardAsync(owner);
        var stranger = await _factory.CreateAuthenticatedClientAsync();

        var export = await stranger.GetFromJsonAsync<PersonalDataExport>(Url);

        Assert.Empty(export!.CreditCards);
        Assert.Empty(export.Transactions);
    }

    [Fact]
    public async Task Export_RequiresSigningIn()
    {
        var response = await _factory.CreateClient().GetAsync(Url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
