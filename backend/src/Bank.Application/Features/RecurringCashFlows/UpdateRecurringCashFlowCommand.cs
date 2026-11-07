using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Finance;
using Bank.Domain.CashFlows;
using MediatR;

namespace Bank.Application.Features.RecurringCashFlows;

/// <param name="Version">The version the client edited, taken from the If-Match header.</param>
public sealed record UpdateRecurringCashFlowCommand(long Id, RecurringCashFlowRequest Item, string Version)
    : IRequest<RecurringCashFlowResponse>;

public sealed class UpdateRecurringCashFlowCommandHandler
    : IRequestHandler<UpdateRecurringCashFlowCommand, RecurringCashFlowResponse>
{
    private readonly IRecurringCashFlowRepository _items;
    private readonly ICreditCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public UpdateRecurringCashFlowCommandHandler(
        IRecurringCashFlowRepository items, ICreditCardRepository cards, ICurrentUser currentUser)
    {
        _items = items;
        _cards = cards;
        _currentUser = currentUser;
    }

    public async Task<RecurringCashFlowResponse> Handle(UpdateRecurringCashFlowCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var item = RecurringCashFlowMapping.ToDomain(command.Id, command.Item);
        await RecurringCashFlowMapping.EnsureCardIsUsableAsync(item, userId, _cards, ct);

        var newVersion = await _items.UpdateAsync(userId, item, RowVersion.Decode(command.Version), ct);
        if (newVersion is null)
            throw await _items.ExistsAsync(userId, command.Id, ct)
                ? new ConflictException("Kayıt siz düzenlerken değiştirildi. Güncel hâlini yükleyip tekrar deneyin.")
                : new NotFoundException(RecurringCashFlowMapping.NotFoundMessage);

        return RecurringCashFlowMapping.ToResponse(new Versioned<RecurringCashFlow>(item, newVersion));
    }
}
