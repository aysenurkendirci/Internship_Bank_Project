using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Finance;
using Bank.Contracts.Payments;
using Bank.Domain.Accounts;
using Bank.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Bank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class TransferTests
{
    private const string Url = "/api/transfers";

    // Every test user is registered as "Test Kullanıcı"; the recipient name is typed differently on purpose.
    private const string RecipientName = "TEST KULLANICI";

    private readonly ApiFactory _factory;

    public TransferTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Transfer_MovesTheMoney_AndRecordsBothSidesInTheLedger()
    {
        var (sender, source, _) = await UserWithAccountAsync(1_000m);
        var (receiver, target, targetIban) = await UserWithAccountAsync(0m);

        var response = await SendAsync(sender, Transfer(source, targetIban, 250.50m), "key-1");
        var transfer = await response.Content.ReadFromJsonAsync<TransferResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"{Url}/{transfer!.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(749.50m, transfer.SourceBalanceAfter);
        Assert.Equal(749.50m, await BalanceAsync(sender, source));
        Assert.Equal(250.50m, await BalanceAsync(receiver, target));

        var debit = Assert.Single((await StatementAsync(sender, source)).Items);
        var credit = Assert.Single((await StatementAsync(receiver, target)).Items);
        Assert.Equal(("Debit", 250.50m, targetIban), (debit.Side, debit.Amount, debit.CounterpartyIban));
        Assert.Equal(("Credit", 250.50m, transfer.SourceIban), (credit.Side, credit.Amount, credit.CounterpartyIban));
        Assert.Equal(transfer.Id, credit.TransferId);
    }

    [Fact]
    public async Task Retry_WithTheSameKey_ReturnsTheOriginal_AndSendsTheMoneyOnce()
    {
        var (sender, source, _) = await UserWithAccountAsync(1_000m);
        var (_, _, targetIban) = await UserWithAccountAsync(0m);
        var request = Transfer(source, targetIban, 100m);

        var first = await SendAsync(sender, request, "retry-key");
        var retry = await SendAsync(sender, request with { RecipientName = "test kullanici" }, "retry-key");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal("true", retry.Headers.GetValues("Idempotent-Replayed").Single());
        Assert.Equal(
            (await first.Content.ReadFromJsonAsync<TransferResponse>())!.Id,
            (await retry.Content.ReadFromJsonAsync<TransferResponse>())!.Id);
        Assert.Equal(900m, await BalanceAsync(sender, source));
    }

    [Fact]
    public async Task SameKey_ForADifferentRequest_Returns422()
    {
        var (sender, source, _) = await UserWithAccountAsync(1_000m);
        var (_, _, targetIban) = await UserWithAccountAsync(0m);

        await SendAsync(sender, Transfer(source, targetIban, 100m), "reused-key");
        var response = await SendAsync(sender, Transfer(source, targetIban, 999m), "reused-key");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(900m, await BalanceAsync(sender, source));
    }

    [Fact]
    public async Task IdempotencyKeys_BelongToTheUser()
    {
        var (first, firstSource, _) = await UserWithAccountAsync(100m);
        var (second, secondSource, _) = await UserWithAccountAsync(100m);
        var (_, _, targetIban) = await UserWithAccountAsync(0m);

        var a = await SendAsync(first, Transfer(firstSource, targetIban, 10m), "shared-key");
        var b = await SendAsync(second, Transfer(secondSource, targetIban, 10m), "shared-key");

        Assert.Equal(HttpStatusCode.Created, a.StatusCode);
        Assert.Equal(HttpStatusCode.Created, b.StatusCode);
    }

    [Fact]
    public async Task WithoutAnIdempotencyKey_Returns400()
    {
        var (sender, source, _) = await UserWithAccountAsync(100m);
        var (_, _, targetIban) = await UserWithAccountAsync(0m);

        var response = await sender.PostAsJsonAsync(Url, Transfer(source, targetIban, 10m));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("idempotencyKey", problem!.Errors.Keys);
    }

    [Fact]
    public async Task MistypedIban_IsRefusedBeforeAnyLookup()
    {
        var (sender, source, _) = await UserWithAccountAsync(100m);

        var response = await SendAsync(sender, Transfer(source, "TR330006100519786457841327", 10m), "typo");
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["Alıcı IBAN geçerli değil."], problem!.Errors["targetIban"]);
    }

    [Theory]
    [InlineData(100.01, "Test Kullanıcı")] // more than the balance
    [InlineData(10, "Başka Biri")] // name does not match the IBAN holder
    public async Task RefusedTransfer_ChangesNothing(decimal amount, string recipientName)
    {
        var (sender, source, _) = await UserWithAccountAsync(100m);
        var (receiver, target, targetIban) = await UserWithAccountAsync(0m);

        var response = await SendAsync(sender, Transfer(source, targetIban, amount) with { RecipientName = recipientName }, "refused");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(100m, await BalanceAsync(sender, source));
        Assert.Equal(0m, await BalanceAsync(receiver, target));
        Assert.Empty((await StatementAsync(sender, source)).Items);
    }

    [Fact]
    public async Task SomeoneElsesAccount_CannotBeTheSource()
    {
        var (_, victimAccount, _) = await UserWithAccountAsync(1_000m);
        var (thief, _, thiefIban) = await UserWithAccountAsync(0m);

        var response = await SendAsync(thief, Transfer(victimAccount, thiefIban, 500m), "steal");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentTransfers_NeverSpendTheSameBalanceTwice()
    {
        var (sender, source, _) = await UserWithAccountAsync(100m);
        var (receiver, target, targetIban) = await UserWithAccountAsync(0m);

        // Ten transfers of 30 race for a balance of 100: exactly three can succeed.
        var responses = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(i => SendAsync(sender, Transfer(source, targetIban, 30m), $"race-{i}")));

        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest));
        Assert.Equal(10m, await BalanceAsync(sender, source));
        Assert.Equal(90m, await BalanceAsync(receiver, target));
    }

    [Fact]
    public async Task ConcurrentRetries_WithOneKey_SendTheMoneyOnce()
    {
        var (sender, source, _) = await UserWithAccountAsync(100m);
        var (_, _, targetIban) = await UserWithAccountAsync(0m);
        var request = Transfer(source, targetIban, 40m);

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => SendAsync(sender, request, "double-click")));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));
        Assert.Equal(60m, await BalanceAsync(sender, source));
    }

    [Fact]
    public async Task OppositeTransfersInParallel_DoNotDeadlock_AndKeepTheTotal()
    {
        var (alice, aliceAccount, aliceIban) = await UserWithAccountAsync(500m);
        var (bob, bobAccount, bobIban) = await UserWithAccountAsync(500m);

        var sends = Enumerable.Range(0, 10).SelectMany(i => new[]
        {
            SendAsync(alice, Transfer(aliceAccount, bobIban, 7m), $"a-{i}"),
            SendAsync(bob, Transfer(bobAccount, aliceIban, 5m), $"b-{i}")
        });
        var responses = await Task.WhenAll(sends);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(480m, await BalanceAsync(alice, aliceAccount));
        Assert.Equal(520m, await BalanceAsync(bob, bobAccount));
    }

    [Fact]
    public async Task Ledger_DebitsAlwaysEqualCredits()
    {
        var (sender, source, _) = await UserWithAccountAsync(1_000m);
        var (_, _, targetIban) = await UserWithAccountAsync(0m);
        for (var i = 0; i < 3; i++)
            await SendAsync(sender, Transfer(source, targetIban, 12.34m), $"ledger-{i}");

        await using var connection = new SqlConnection(ApiFactory.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*) FROM (
                SELECT TransferId
                FROM payments.LedgerEntries
                GROUP BY TransferId
                HAVING SUM(CASE Side WHEN 'Debit' THEN Amount ELSE -Amount END) <> 0 OR COUNT(*) <> 2
            ) unbalanced
            """;

        Assert.Equal(0, (int)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task OthersCannotReadATransferOrAStatement()
    {
        var (sender, source, _) = await UserWithAccountAsync(100m);
        var (stranger, _, strangerIban) = await UserWithAccountAsync(0m);
        var sent = await (await SendAsync(sender, Transfer(source, strangerIban, 10m), "private"))
            .Content.ReadFromJsonAsync<TransferResponse>();

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"{Url}/{sent!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/accounts/{source}/statement")).StatusCode);
    }

    [Fact]
    public async Task OpenedAccount_HasAValidIban_AndCanReceiveMoney()
    {
        var (sender, source, _) = await UserWithAccountAsync(100m);
        var owner = await _factory.CreateAuthenticatedClientAsync();

        var response = await owner.PostAsJsonAsync("/api/accounts", new OpenAccountRequest("TRY"));
        var opened = await response.Content.ReadFromJsonAsync<AccountResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(Iban.TryParse(opened!.Iban, out _));
        Assert.Equal(("Demand", 0m, "Active"), (opened.AccountType, opened.Balance, opened.Status));

        var transfer = await SendAsync(sender, Transfer(source, opened.Iban, 25m), "to-new-account");
        Assert.Equal(HttpStatusCode.Created, transfer.StatusCode);
        Assert.Equal(25m, await BalanceAsync(owner, opened.Id));
    }

    private async Task<(HttpClient Client, long AccountId, string Iban)> UserWithAccountAsync(decimal balance)
    {
        var (client, userId) = await _factory.CreateUserAsync();
        var (accountId, iban) = await TestData.AddAccountAsync(userId, balance);
        return (client, accountId, iban);
    }

    private static TransferRequest Transfer(long sourceAccountId, string targetIban, decimal amount)
        => new(sourceAccountId, targetIban, RecipientName, amount, "Kira payı");

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, TransferRequest request, string idempotencyKey)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, Url) { Content = JsonContent.Create(request) };
        message.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(message);
    }

    private static async Task<decimal> BalanceAsync(HttpClient client, long accountId)
        => (await client.GetFromJsonAsync<List<AccountResponse>>("/api/accounts"))!.Single(a => a.Id == accountId).Balance;

    private static async Task<PagedResponse<StatementEntryResponse>> StatementAsync(HttpClient client, long accountId)
        => (await client.GetFromJsonAsync<PagedResponse<StatementEntryResponse>>($"/api/accounts/{accountId}/statement"))!;
}
