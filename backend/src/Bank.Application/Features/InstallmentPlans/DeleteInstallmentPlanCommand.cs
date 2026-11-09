using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using MediatR;

namespace Bank.Application.Features.InstallmentPlans;

/// <summary>Soft-deletes an installment plan.</summary>
public sealed record DeleteInstallmentPlanCommand(long Id) : IRequest;

public sealed class DeleteInstallmentPlanCommandHandler : IRequestHandler<DeleteInstallmentPlanCommand>
{
    private readonly IInstallmentPlanRepository _plans;
    private readonly ICurrentUser _currentUser;

    public DeleteInstallmentPlanCommandHandler(IInstallmentPlanRepository plans, ICurrentUser currentUser)
    {
        _plans = plans;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteInstallmentPlanCommand command, CancellationToken ct)
    {
        if (!await _plans.SoftDeleteAsync(_currentUser.UserId, command.Id, ct))
            throw new NotFoundException(InstallmentPlanMapping.NotFoundMessage);
    }
}
