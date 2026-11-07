using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Finance;
using MediatR;

namespace Bank.Application.Features.RecurringCashFlows;

public sealed record GetRecurringCashFlowQuery(long Id) : IRequest<RecurringCashFlowResponse>;

public sealed class GetRecurringCashFlowQueryHandler : IRequestHandler<GetRecurringCashFlowQuery, RecurringCashFlowResponse>
{
    private readonly IRecurringCashFlowRepository _items;
    private readonly ICurrentUser _currentUser;

    public GetRecurringCashFlowQueryHandler(IRecurringCashFlowRepository items, ICurrentUser currentUser)
    {
        _items = items;
        _currentUser = currentUser;
    }

    public async Task<RecurringCashFlowResponse> Handle(GetRecurringCashFlowQuery query, CancellationToken ct)
    {
        var item = await _items.GetAsync(_currentUser.UserId, query.Id, ct)
                   ?? throw new NotFoundException(RecurringCashFlowMapping.NotFoundMessage);
        return RecurringCashFlowMapping.ToResponse(item);
    }
}
