using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Finance;
using MediatR;

namespace Bank.Application.Features.CreditCards;

/// <summary>One of the current customer's credit cards.</summary>
public sealed record GetCreditCardQuery(long Id) : IRequest<CreditCardResponse>;

public sealed class GetCreditCardQueryHandler : IRequestHandler<GetCreditCardQuery, CreditCardResponse>
{
    private readonly ICreditCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public GetCreditCardQueryHandler(ICreditCardRepository cards, ICurrentUser currentUser)
    {
        _cards = cards;
        _currentUser = currentUser;
    }

    public async Task<CreditCardResponse> Handle(GetCreditCardQuery query, CancellationToken ct)
    {
        var card = await _cards.GetAsync(_currentUser.UserId, query.Id, ct)
                   ?? throw new NotFoundException(CreditCardMapping.NotFoundMessage);
        return CreditCardMapping.ToResponse(card);
    }
}
