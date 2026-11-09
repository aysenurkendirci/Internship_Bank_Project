using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using MediatR;

namespace Bank.Application.Features.RecurringCashFlows;

/// <summary>Soft-deletes a recurring item.</summary>
public sealed record DeleteRecurringCashFlowCommand(long Id) : IRequest;

public sealed class DeleteRecurringCashFlowCommandHandler : IRequestHandler<DeleteRecurringCashFlowCommand>
{
    private readonly IRecurringCashFlowRepository _items;
    private readonly ICurrentUser _currentUser;

    public DeleteRecurringCashFlowCommandHandler(IRecurringCashFlowRepository items, ICurrentUser currentUser)
    {
        _items = items;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteRecurringCashFlowCommand command, CancellationToken ct)
    {
        if (!await _items.SoftDeleteAsync(_currentUser.UserId, command.Id, ct))
            throw new NotFoundException(RecurringCashFlowMapping.NotFoundMessage);
    }
}
