using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Finance;
using Bank.Domain.Cards;
using MediatR;

namespace Bank.Application.Features.CreditCards;

/// <param name="Version">The version the client edited, taken from the If-Match header.</param>
public sealed record UpdateCreditCardCommand(long Id, CreditCardRequest Card, string Version) : IRequest<CreditCardResponse>;

public sealed class UpdateCreditCardCommandHandler : IRequestHandler<UpdateCreditCardCommand, CreditCardResponse>
{
    private readonly ICreditCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public UpdateCreditCardCommandHandler(ICreditCardRepository cards, ICurrentUser currentUser)
    {
        _cards = cards;
        _currentUser = currentUser;
    }

    public async Task<CreditCardResponse> Handle(UpdateCreditCardCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var current = await _cards.GetAsync(userId, command.Id, ct)
                      ?? throw new NotFoundException(CreditCardMapping.NotFoundMessage);

        // Installments and card-paid items are stored in the card's currency;
        // switching it underneath them would make their schedules invalid.
        var currencyChanged = current.Value.Limit.Currency.Code != command.Card.CurrencyCode;
        if (currencyChanged && await _cards.IsInUseAsync(userId, command.Id, ct))
            throw new ConflictException("Taksit veya düzenli ödemesi olan kartın para birimi değiştirilemez.");

        var updated = CreditCardMapping.ToDomain(command.Id, command.Card);
        var newVersion = await _cards.UpdateAsync(userId, updated, RowVersion.Decode(command.Version), ct);

        if (newVersion is null)
            throw await _cards.ExistsAsync(userId, command.Id, ct)
                ? new ConflictException("Kart siz düzenlerken değiştirildi. Güncel hâlini yükleyip tekrar deneyin.")
                : new NotFoundException(CreditCardMapping.NotFoundMessage);

        return CreditCardMapping.ToResponse(new Versioned<CreditCard>(updated, newVersion));
    }
}
