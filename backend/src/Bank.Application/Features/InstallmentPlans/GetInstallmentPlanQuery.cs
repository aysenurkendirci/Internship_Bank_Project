using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Finance;
using MediatR;

namespace Bank.Application.Features.InstallmentPlans;

/// <summary>One installment plan with its schedule.</summary>
public sealed record GetInstallmentPlanQuery(long Id) : IRequest<InstallmentPlanResponse>;

public sealed class GetInstallmentPlanQueryHandler : IRequestHandler<GetInstallmentPlanQuery, InstallmentPlanResponse>
{
    private readonly IInstallmentPlanRepository _plans;
    private readonly ICreditCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public GetInstallmentPlanQueryHandler(
        IInstallmentPlanRepository plans, ICreditCardRepository cards, ICurrentUser currentUser)
    {
        _plans = plans;
        _cards = cards;
        _currentUser = currentUser;
    }

    public async Task<InstallmentPlanResponse> Handle(GetInstallmentPlanQuery query, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var plan = await _plans.GetAsync(userId, query.Id, ct)
                   ?? throw new NotFoundException(InstallmentPlanMapping.NotFoundMessage);
        var card = await _cards.GetAsync(userId, plan.CreditCardId, ct)
                   ?? throw InstallmentPlanMapping.MissingCard(plan);

        return InstallmentPlanMapping.ToResponse(plan, card.Value);
    }
}
