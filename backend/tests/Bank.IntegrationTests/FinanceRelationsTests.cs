using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Finance;
using Bank.IntegrationTests.Infrastructure;

namespace Bank.IntegrationTests;

/// <summary>Rules that span cards, recurring items and installment plans.</summary>
[Collection(ApiCollection.Name)]
public sealed class FinanceRelationsTests
{
    private const string CashFlowsUrl = "/api/finance/recurring-cash-flows";
    private const string PlansUrl = "/api/finance/installment-plans";

    private readonly ApiFactory _factory;

    public FinanceRelationsTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Subscription_OnAnotherUsersCard_IsRejectedAsUnknownCard()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var stranger = await _factory.CreateAuthenticatedClientAsync();
        var ownersCard = await CreditCardTests.CreateCardAsync(owner);

        var response = await stranger.PostAsJsonAsync(CashFlowsUrl, Subscription(ownersCard.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Subscriptions_CanBeListedSeparately()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var card = await CreditCardTests.CreateCardAsync(owner);
        await owner.PostAsJsonAsync(CashFlowsUrl, Subscription(card.Id));
        await owner.PostAsJsonAsync(CashFlowsUrl, Salary());

        var subscriptions = await owner.GetFromJsonAsync<List<RecurringCashFlowResponse>>($"{CashFlowsUrl}?kind=Subscription");

        Assert.Equal("Netflix", Assert.Single(subscriptions!).Name);
    }

    [Fact]
    public async Task CardInUse_CannotBeDeletedUntilItsItemsAreRemoved()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var card = await CreditCardTests.CreateCardAsync(owner);
        var plan = await CreatePlanAsync(owner, card.Id);

        var whileInUse = await owner.DeleteAsync($"/api/finance/credit-cards/{card.Id}");
        await owner.DeleteAsync($"{PlansUrl}/{plan.Id}");
        var afterPlanRemoved = await owner.DeleteAsync($"/api/finance/credit-cards/{card.Id}");

        Assert.Equal(HttpStatusCode.Conflict, whileInUse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, afterPlanRemoved.StatusCode);
    }

    [Fact]
    public async Task InstallmentSchedule_FollowsCardStatementsAndAddsUpToTotal()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var card = await CreditCardTests.CreateCardAsync(owner);

        var plan = await CreatePlanAsync(owner, card.Id);

        Assert.Equal(
            [
                new InstallmentResponse(1, new(2026, 4, 20), new(2026, 4, 30), 333.34m),
                new InstallmentResponse(2, new(2026, 5, 20), new(2026, 6, 1), 333.33m),
                new InstallmentResponse(3, new(2026, 6, 20), new(2026, 6, 30), 333.33m)
            ],
            plan.Installments);
    }

    [Fact]
    public async Task InstallmentSchedule_IsRecomputedWhenTheCardChanges()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var card = await CreditCardTests.CreateCardAsync(owner);
        var plan = await CreatePlanAsync(owner, card.Id);

        var put = new HttpRequestMessage(HttpMethod.Put, $"/api/finance/credit-cards/{card.Id}")
        {
            Content = JsonContent.Create(new CreditCardRequest("Bonus", 50_000m, "TRY", StatementDay: 15, PaymentDueDays: 10))
        };
        put.Headers.TryAddWithoutValidation("If-Match", card.Version);
        (await owner.SendAsync(put)).EnsureSuccessStatusCode();

        var reloaded = await owner.GetFromJsonAsync<InstallmentPlanResponse>($"{PlansUrl}/{plan.Id}");

        Assert.All(reloaded!.Installments, i => Assert.Equal(15, i.StatementDate.Day));
    }

    private static async Task<InstallmentPlanResponse> CreatePlanAsync(HttpClient client, long cardId)
    {
        var response = await client.PostAsJsonAsync(
            PlansUrl, new InstallmentPlanRequest(cardId, "Dizüstü bilgisayar", 1000m, 3, new DateOnly(2026, 3, 25)));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InstallmentPlanResponse>())!;
    }

    private static RecurringCashFlowRequest Subscription(long cardId)
        => new("Netflix", "Subscription", "Eğlence", 229.99m, "TRY", "Monthly", 1, new DateOnly(2026, 1, 31), null, cardId);

    private static RecurringCashFlowRequest Salary()
        => new("Maaş", "Income", "Maaş", 45_000m, "TRY", "Monthly", 1, new DateOnly(2026, 1, 15), null, null);
}
