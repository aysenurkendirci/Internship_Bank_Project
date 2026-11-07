using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Finance;
using Bank.Domain.Cards;
using Bank.Domain.ValueObjects;
using MediatR;

namespace Bank.Application.Features.InstallmentPlans;

public sealed record CreateInstallmentPlanCommand(InstallmentPlanRequest Plan) : IRequest<InstallmentPlanResponse>;

public sealed class CreateInstallmentPlanCommandHandler : IRequestHandler<CreateInstallmentPlanCommand, InstallmentPlanResponse>
{
    private readonly IInstallmentPlanRepository _plans;
    private readonly ICreditCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public CreateInstallmentPlanCommandHandler(
        IInstallmentPlanRepository plans, ICreditCardRepository cards, ICurrentUser currentUser)
    {
        _plans = plans;
        _cards = cards;
        _currentUser = currentUser;
    }

    public async Task<InstallmentPlanResponse> Handle(CreateInstallmentPlanCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var request = command.Plan;
        var card = await _cards.GetAsync(userId, request.CreditCardId!.Value, ct)
                   ?? throw new BadRequestException("Seçilen kart bulunamadı.");

        // A plan is always in its card's currency; the request carries no currency of its own.
        var plan = new InstallmentPlan(
            id: 0,
            card.Value.Id,
            request.Description,
            new Money(request.TotalAmount, card.Value.Limit.Currency),
            request.InstallmentCount,
            request.PurchaseDate!.Value);

        var created = await _plans.AddAsync(userId, plan, ct);
        return InstallmentPlanMapping.ToResponse(created, card.Value);
    }
}
