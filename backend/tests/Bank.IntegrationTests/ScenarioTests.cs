using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Finance;
using Bank.IntegrationTests.Infrastructure;

namespace Bank.IntegrationTests;

/// <remarks>The API clock is pinned to 31 March 2026, so the outlook starts on 1 April.</remarks>
[Collection(ApiCollection.Name)]
public sealed class ScenarioTests
{
    private const string Url = "/api/finance/scenarios/installments";

    private readonly ApiFactory _factory;

    public ScenarioTests(ApiFactory factory) => _factory = factory;

    /// <summary>
    /// Same tight budget as the domain test: 1 000 opening balance, 20 000 salary on the 1st,
    /// 18 400 costs on the 2nd, card closing on the 10th and due 10 days later. The API must
    /// produce the same figures the domain test asserts.
    /// </summary>
    private async Task<(HttpClient Client, long CardId)> TightBudgetAsync()
    {
        var (client, userId) = await _factory.CreateUserAsync();
        await TestData.AddTryAccountAsync(userId, 1_000m);

        await client.PostAsJsonAsync("/api/finance/recurring-cash-flows",
            new RecurringCashFlowRequest("Maaş", "Income", "Maaş", 20_000m, "TRY", "Monthly", 1, new(2026, 1, 1), null, null));
        await client.PostAsJsonAsync("/api/finance/recurring-cash-flows",
            new RecurringCashFlowRequest("Giderler", "Expense", "Yaşam", 18_400m, "TRY", "Monthly", 1, new(2026, 1, 2), null, null));
        var card = await CreditCardTests.CreateCardAsync(client,
            new CreditCardRequest("Bonus", 100_000m, "TRY", StatementDay: 10, PaymentDueDays: 10));

        return (client, card.Id);
    }

    private static InstallmentScenarioRequest Laptop(long cardId, params PurchaseOptionRequest[] options)
        => new(cardId, "Laptop", new DateOnly(2026, 4, 5), options);

    [Fact]
    public async Task Compare_ShowsWhichOptionPushesTheBalanceBelowZero()
    {
        var (client, cardId) = await TightBudgetAsync();

        var response = await client.PostAsJsonAsync(Url, Laptop(cardId, new PurchaseOptionRequest(3, 9_000m), new PurchaseOptionRequest(6, 9_000m)));
        var result = await response.Content.ReadFromJsonAsync<ScenarioComparisonResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new DateOnly(2026, 9, 30), result!.To);
        Assert.Equal(0, result.Baseline.MonthsBelowZero);

        var (three, six) = (result.Options[0].Metrics, result.Options[1].Metrics);
        Assert.Equal(5, three.MonthsBelowZero);
        Assert.Equal(-3_200m, three.LowestBalance);
        Assert.Equal(0, six.MonthsBelowZero);
        Assert.Equal([3_000m, 3_000m, 3_000m], result.Options[0].Installments.Select(i => i.Amount));
    }

    [Fact]
    public async Task Compare_DoesNotSaveAnything()
    {
        var (client, cardId) = await TightBudgetAsync();

        await client.PostAsJsonAsync(Url, Laptop(cardId, new PurchaseOptionRequest(3, 9_000m)));

        var plans = await client.GetFromJsonAsync<List<InstallmentPlanResponse>>("/api/finance/installment-plans");
        Assert.Empty(plans!);
    }

    [Fact]
    public async Task Compare_OnAnotherUsersCard_Returns400()
    {
        var (_, ownersCardId) = await TightBudgetAsync();
        var stranger = await _factory.CreateAuthenticatedClientAsync();

        var response = await stranger.PostAsJsonAsync(Url, Laptop(ownersCardId, new PurchaseOptionRequest(3, 9_000m)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Compare_WithoutOptions_Returns400()
    {
        var (client, cardId) = await TightBudgetAsync();

        var response = await client.PostAsJsonAsync(Url, Laptop(cardId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Compare_WithInvalidOption_Returns400()
    {
        var (client, cardId) = await TightBudgetAsync();

        var response = await client.PostAsJsonAsync(Url, Laptop(cardId, new PurchaseOptionRequest(0, 9_000m)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
