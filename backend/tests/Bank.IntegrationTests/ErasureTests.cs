using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Account;
using Bank.Contracts.Auth;
using Bank.Contracts.Finance;
using Bank.Contracts.Payments;
using Bank.IntegrationTests.Infrastructure;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace Bank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ErasureTests
{
    private readonly ApiFactory _factory;

    public ErasureTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Erasure_DeletesFinanceDataWithItsHistory_AndLeavesOthersAlone()
    {
        var (me, myId, _) = await CustomerWithFinanceDataAsync();
        var (other, otherId, _) = await CustomerWithFinanceDataAsync();

        // Updating the card puts a second version into the history table, which a plain DELETE would leave behind.
        Assert.True(await CountAsync("finance.CreditCardsHistory", myId) > 0);

        var response = await EraseAsync(me);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        foreach (var table in FinanceTables)
            Assert.Equal(0, await CountAsync(table, myId));

        foreach (var table in FinanceTables.Where(t => t != "finance.RecurringCashFlowsHistory" && t != "finance.InstallmentPlansHistory"))
            Assert.True(await CountAsync(table, otherId) > 0, $"{table} of the other customer was touched");
        Assert.Single(await other.GetFromJsonAsync<List<CreditCardResponse>>("/api/finance/credit-cards") ?? []);
    }

    [Fact]
    public async Task Erasure_StripsThePerson_ButKeepsAccountsAndTheLedger()
    {
        var registration = TestUsers.NewRegistration();
        var (me, myId) = await _factory.CreateUserAsync(registration);
        var (accountId, _) = await TestData.AddTryAccountAsync(myId, 100m);
        var (_, otherId) = await _factory.CreateUserAsync();
        var (_, otherIban) = await TestData.AddTryAccountAsync(otherId, 0m);
        await SendMoneyAsync(me, accountId, otherIban, 100m);
        var cardId = await TestData.AddCardAsync(myId, accountId);

        await EraseAsync(me);

        await using var connection = Open();
        var user = await connection.QuerySingleAsync<(string TcNo, string FirstName, string LastName, string Email, string Phone)>(
            "SELECT TcNo, FirstName, LastName, Email, Phone FROM dbo.Users WHERE UserId = @Id", new { Id = myId });
        Assert.DoesNotContain(registration.TcNo, user.TcNo);
        Assert.DoesNotContain(registration.Email, user.Email);
        Assert.Equal(("Silinmiş", "Kullanıcı"), (user.FirstName, user.LastName));

        Assert.Equal("Closed", await connection.QuerySingleAsync<string>("SELECT Status FROM dbo.Accounts WHERE AccountId = @Id", new { Id = accountId }));
        Assert.Equal("Cancelled", await connection.QuerySingleAsync<string>("SELECT Status FROM dbo.Cards WHERE CardId = @Id", new { Id = cardId }));
        Assert.Equal(2, await connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM payments.LedgerEntries WHERE AccountId = @Id OR TransferId IN (SELECT TransferId FROM payments.LedgerEntries WHERE AccountId = @Id)",
            new { Id = accountId }));
    }

    [Fact]
    public async Task TheIdentityNumber_IsKeptApartForTheRetentionPeriod()
    {
        var registration = TestUsers.NewRegistration();
        var (me, myId) = await _factory.CreateUserAsync(registration);

        await EraseAsync(me);

        await using var connection = Open();
        var kept = await connection.QuerySingleAsync<(string TcNo, DateTime ErasedAt)>(
            "SELECT TcNo, ErasedAt FROM compliance.ErasedIdentities WHERE UserId = @Id", new { Id = myId });
        Assert.Equal(registration.TcNo, kept.TcNo);
    }

    [Fact]
    public async Task AfterErasure_NothingCanSignInOrUseAnOldToken()
    {
        var registration = TestUsers.NewRegistration();
        var (me, _) = await _factory.CreateUserAsync(registration);
        var signIn = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(registration.TcNo, TestUsers.Password));
        var refreshCookie = signIn.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("refresh_token=")).Split(';')[0];

        await EraseAsync(me);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(registration.TcNo, TestUsers.Password))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await me.GetAsync("/api/finance/credit-cards")).StatusCode);

        var refresh = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refresh.Headers.Add("X-Requested-By", "bank-web");
        refresh.Headers.Add("Cookie", refreshCookie);
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(refresh)).StatusCode);
    }

    [Fact]
    public async Task WrongPassword_ErasesNothing()
    {
        var (me, myId, _) = await CustomerWithFinanceDataAsync();

        var response = await me.SendAsync(Erase("not-my-password"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(await CountAsync("finance.CreditCards", myId) > 0);
        Assert.Equal(HttpStatusCode.OK, (await me.GetAsync("/api/finance/credit-cards")).StatusCode);
    }

    [Fact]
    public async Task AccountWithMoney_MustBeEmptiedFirst()
    {
        var (me, myId) = await _factory.CreateUserAsync();
        await TestData.AddTryAccountAsync(myId, 0.01m);

        var response = await EraseAsync(me);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await me.GetAsync("/api/accounts")).StatusCode);
    }

    [Fact]
    public async Task ErasingTwice_IsHarmless()
    {
        var (me, myId) = await _factory.CreateUserAsync();
        await EraseAsync(me);

        await using var connection = Open();
        await connection.ExecuteAsync("EXEC compliance.EraseUser @UserId = @Id, @Now = '2026-03-31'", new { Id = myId });

        Assert.Equal(1, await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM compliance.ErasedIdentities WHERE UserId = @Id", new { Id = myId }));
    }

    [Fact]
    public async Task WithoutSigningIn_Returns401()
    {
        var response = await _factory.CreateClient().SendAsync(Erase(TestUsers.Password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static readonly string[] FinanceTables =
    [
        "finance.CreditCards", "finance.CreditCardsHistory", "finance.RecurringCashFlows", "finance.RecurringCashFlowsHistory",
        "finance.InstallmentPlans", "finance.InstallmentPlansHistory", "finance.Transactions", "finance.ImportBatches"
    ];

    private async Task<(HttpClient Client, long UserId, long CardId)> CustomerWithFinanceDataAsync()
    {
        var (client, userId) = await _factory.CreateUserAsync();

        var card = await (await client.PostAsJsonAsync("/api/finance/credit-cards", new CreditCardRequest("Bonus", 50_000m, "TRY", 20, 10)))
            .Content.ReadFromJsonAsync<CreditCardResponse>();
        var update = new HttpRequestMessage(HttpMethod.Put, $"/api/finance/credit-cards/{card!.Id}")
        {
            Content = JsonContent.Create(new CreditCardRequest("Bonus", 75_000m, "TRY", 20, 10))
        };
        update.Headers.TryAddWithoutValidation("If-Match", $"\"{card.Version}\"");
        (await client.SendAsync(update)).EnsureSuccessStatusCode();

        (await client.PostAsJsonAsync("/api/finance/recurring-cash-flows",
            new RecurringCashFlowRequest("Kira", "Expense", "Konut", 9_000m, "TRY", "Monthly", 1, new(2026, 1, 1), null, null))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/finance/installment-plans",
            new InstallmentPlanRequest(card.Id, "Telefon", 12_000m, 6, new(2026, 3, 1)))).EnsureSuccessStatusCode();
        (await client.PostAsync("/api/finance/transactions/demo?months=3", null)).EnsureSuccessStatusCode();

        return (client, userId, card.Id);
    }

    private static HttpRequestMessage Erase(string password)
        => new(HttpMethod.Delete, "/api/me") { Content = JsonContent.Create(new EraseAccountRequest(password)) };

    private static Task<HttpResponseMessage> EraseAsync(HttpClient client) => client.SendAsync(Erase(TestUsers.Password));

    private static Task SendMoneyAsync(HttpClient client, long sourceAccountId, string targetIban, decimal amount)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/transfers")
        {
            Content = JsonContent.Create(new TransferRequest(sourceAccountId, targetIban, "Test Kullanıcı", amount, "Erasure testi"))
        };
        message.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        return client.SendAsync(message).ContinueWith(t => t.Result.EnsureSuccessStatusCode());
    }

    private static async Task<int> CountAsync(string table, long userId)
    {
        await using var connection = Open();
        return await connection.QuerySingleAsync<int>($"SELECT COUNT(*) FROM {table} WHERE UserId = @UserId", new { UserId = userId });
    }

    private static SqlConnection Open() => new(ApiFactory.ConnectionString);
}
