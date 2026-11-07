using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Contracts.Finance;
using MediatR;

namespace Bank.Application.Features.CreditCards;

public sealed record ListCreditCardsQuery : IRequest<IReadOnlyList<CreditCardResponse>>;

public sealed class ListCreditCardsQueryHandler : IRequestHandler<ListCreditCardsQuery, IReadOnlyList<CreditCardResponse>>
{
    private readonly ICreditCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public ListCreditCardsQueryHandler(ICreditCardRepository cards, ICurrentUser currentUser)
    {
        _cards = cards;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CreditCardResponse>> Handle(ListCreditCardsQuery query, CancellationToken ct)
        => (await _cards.ListAsync(_currentUser.UserId, ct)).Select(CreditCardMapping.ToResponse).ToList();
}
