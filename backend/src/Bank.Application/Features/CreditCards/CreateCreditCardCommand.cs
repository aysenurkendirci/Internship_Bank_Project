using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Contracts.Finance;
using MediatR;

namespace Bank.Application.Features.CreditCards;

public sealed record CreateCreditCardCommand(CreditCardRequest Card) : IRequest<CreditCardResponse>;

public sealed class CreateCreditCardCommandHandler : IRequestHandler<CreateCreditCardCommand, CreditCardResponse>
{
    private readonly ICreditCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public CreateCreditCardCommandHandler(ICreditCardRepository cards, ICurrentUser currentUser)
    {
        _cards = cards;
        _currentUser = currentUser;
    }

    public async Task<CreditCardResponse> Handle(CreateCreditCardCommand command, CancellationToken ct)
    {
        var created = await _cards.AddAsync(_currentUser.UserId, CreditCardMapping.ToDomain(id: 0, command.Card), ct);
        return CreditCardMapping.ToResponse(created);
    }
}
