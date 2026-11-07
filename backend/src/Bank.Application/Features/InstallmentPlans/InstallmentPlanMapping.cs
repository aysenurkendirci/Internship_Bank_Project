using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Finance;
using Bank.Domain.Cards;

namespace Bank.Application.Features.InstallmentPlans;

/// <remarks>
/// Schedules are computed from the card on every read rather than stored, so they always follow
/// the card's current statement day and payment period.
/// </remarks>
internal static class InstallmentPlanMapping
{
    public const string NotFoundMessage = "Taksit planı bulunamadı.";

    /// <summary>All of a user's plans with their schedules; shared by the list endpoint and the data export.</summary>
    public static async Task<IReadOnlyList<InstallmentPlanResponse>> ListAsync(
        long userId, IInstallmentPlanRepository plans, ICreditCardRepository cards, CancellationToken ct)
    {
        var stored = await plans.ListAsync(userId, ct);
        var cardsById = (await cards.ListAsync(userId, ct)).ToDictionary(c => c.Value.Id, c => c.Value);

        return stored
            .Select(plan => ToResponse(plan, cardsById.TryGetValue(plan.CreditCardId, out var card) ? card : throw MissingCard(plan)))
            .ToList();
    }

    // Card deletion is refused while plans reference it, so reaching this means the data is inconsistent.
    public static InvalidOperationException MissingCard(InstallmentPlan plan)
        => new($"Installment plan {plan.Id} references card {plan.CreditCardId}, which is not available.");

    public static InstallmentPlanResponse ToResponse(InstallmentPlan plan, CreditCard card)
        => new(
            plan.Id,
            plan.CreditCardId,
            plan.Description,
            plan.TotalAmount.Amount,
            plan.TotalAmount.Currency.Code,
            plan.InstallmentCount,
            plan.PurchaseDate,
            plan.ScheduleOn(card)
                .Select(i => new InstallmentResponse(i.Number, i.StatementDate, i.DueDate, i.Amount.Amount))
                .ToList());
}
