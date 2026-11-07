using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Finance;
using Bank.Domain.Cards;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Services.Finance;

/// <remarks>
/// Schedules are computed from the card on every read rather than stored, so they always follow
/// the card's current statement day and payment period.
/// </remarks>
public sealed class InstallmentPlanService : IInstallmentPlanService
{
    private const string NotFoundMessage = "Taksit planı bulunamadı.";

    private readonly IInstallmentPlanRepository _plans;
    private readonly ICreditCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public InstallmentPlanService(
        IInstallmentPlanRepository plans, ICreditCardRepository cards, ICurrentUser currentUser)
    {
        _plans = plans;
        _cards = cards;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<InstallmentPlanResponse>> ListAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var plans = await _plans.ListAsync(userId, ct);
        var cards = (await _cards.ListAsync(userId, ct)).ToDictionary(c => c.Value.Id, c => c.Value);

        return plans.Select(plan => ToResponse(plan, CardOf(plan, cards))).ToList();
    }

    public async Task<InstallmentPlanResponse> GetAsync(long id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var plan = await _plans.GetAsync(userId, id, ct)
                   ?? throw new NotFoundException(NotFoundMessage);
        var card = await _cards.GetAsync(userId, plan.CreditCardId, ct)
                   ?? throw MissingCard(plan);

        return ToResponse(plan, card.Value);
    }

    public async Task<InstallmentPlanResponse> CreateAsync(InstallmentPlanRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var card = await _cards.GetAsync(userId, request.CreditCardId!.Value, ct)
                   ?? throw new BadRequestException("Seçilen kart bulunamadı.");

        var plan = new InstallmentPlan(
            id: 0,
            card.Value.Id,
            request.Description,
            new Money(request.TotalAmount, card.Value.Limit.Currency),
            request.InstallmentCount,
            request.PurchaseDate!.Value);

        var created = await _plans.AddAsync(userId, plan, ct);
        return ToResponse(created, card.Value);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        if (!await _plans.SoftDeleteAsync(_currentUser.UserId, id, ct))
            throw new NotFoundException(NotFoundMessage);
    }

    private static CreditCard CardOf(InstallmentPlan plan, IReadOnlyDictionary<long, CreditCard> cards)
        => cards.TryGetValue(plan.CreditCardId, out var card) ? card : throw MissingCard(plan);

    // Card deletion is refused while plans reference it, so reaching this means the data is inconsistent.
    private static InvalidOperationException MissingCard(InstallmentPlan plan)
        => new($"Installment plan {plan.Id} references card {plan.CreditCardId}, which is not available.");

    private static InstallmentPlanResponse ToResponse(InstallmentPlan plan, CreditCard card)
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
