using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Contracts.Finance;
using MediatR;

namespace Bank.Application.Features.RecurringCashFlows;

/// <summary>Adds a regular income, expense or subscription.</summary>
public sealed record CreateRecurringCashFlowCommand(RecurringCashFlowRequest Item) : IRequest<RecurringCashFlowResponse>;

public sealed class CreateRecurringCashFlowCommandHandler
    : IRequestHandler<CreateRecurringCashFlowCommand, RecurringCashFlowResponse>
{
    private readonly IRecurringCashFlowRepository _items;
    private readonly ICreditCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public CreateRecurringCashFlowCommandHandler(
        IRecurringCashFlowRepository items, ICreditCardRepository cards, ICurrentUser currentUser)
    {
        _items = items;
        _cards = cards;
        _currentUser = currentUser;
    }

    public async Task<RecurringCashFlowResponse> Handle(CreateRecurringCashFlowCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var item = RecurringCashFlowMapping.ToDomain(id: 0, command.Item);
        await RecurringCashFlowMapping.EnsureCardIsUsableAsync(item, userId, _cards, ct);

        return RecurringCashFlowMapping.ToResponse(await _items.AddAsync(userId, item, ct));
    }
}
