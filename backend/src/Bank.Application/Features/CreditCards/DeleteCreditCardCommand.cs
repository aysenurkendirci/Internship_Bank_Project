using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using MediatR;

namespace Bank.Application.Features.CreditCards;

public sealed record DeleteCreditCardCommand(long Id) : IRequest;

public sealed class DeleteCreditCardCommandHandler : IRequestHandler<DeleteCreditCardCommand>
{
    private readonly ICreditCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public DeleteCreditCardCommandHandler(ICreditCardRepository cards, ICurrentUser currentUser)
    {
        _cards = cards;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCreditCardCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (await _cards.SoftDeleteIfUnusedAsync(userId, command.Id, ct))
            return;

        throw await _cards.ExistsAsync(userId, command.Id, ct)
            ? new ConflictException("Bu karta bağlı taksit veya düzenli ödeme var. Önce onları kaldırın.")
            : new NotFoundException(CreditCardMapping.NotFoundMessage);
    }
}
