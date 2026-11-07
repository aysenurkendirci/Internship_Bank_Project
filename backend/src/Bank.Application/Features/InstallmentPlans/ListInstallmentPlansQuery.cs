using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Contracts.Finance;
using MediatR;

namespace Bank.Application.Features.InstallmentPlans;

public sealed record ListInstallmentPlansQuery : IRequest<IReadOnlyList<InstallmentPlanResponse>>;

public sealed class ListInstallmentPlansQueryHandler
    : IRequestHandler<ListInstallmentPlansQuery, IReadOnlyList<InstallmentPlanResponse>>
{
    private readonly IInstallmentPlanRepository _plans;
    private readonly ICreditCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public ListInstallmentPlansQueryHandler(
        IInstallmentPlanRepository plans, ICreditCardRepository cards, ICurrentUser currentUser)
    {
        _plans = plans;
        _cards = cards;
        _currentUser = currentUser;
    }

    public Task<IReadOnlyList<InstallmentPlanResponse>> Handle(ListInstallmentPlansQuery query, CancellationToken ct)
        => InstallmentPlanMapping.ListAsync(_currentUser.UserId, _plans, _cards, ct);
}
