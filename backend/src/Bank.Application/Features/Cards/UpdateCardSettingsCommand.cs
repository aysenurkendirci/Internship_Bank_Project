using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Cards;
using Bank.Domain.Cards;
using MediatR;

namespace Bank.Application.Features.Cards;

/// <summary>Contactless and online use on or off, and the daily and monthly spending limits.</summary>
public sealed record UpdateCardSettingsCommand(long CardId, CardSettingsRequest Settings) : IRequest<CardSettingsResponse>;

public sealed class UpdateCardSettingsCommandHandler : IRequestHandler<UpdateCardSettingsCommand, CardSettingsResponse>
{
    private readonly IBankCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public UpdateCardSettingsCommandHandler(IBankCardRepository cards, ICurrentUser currentUser)
    {
        _cards = cards;
        _currentUser = currentUser;
    }

    public async Task<CardSettingsResponse> Handle(UpdateCardSettingsCommand command, CancellationToken ct)
    {
        var card = await _cards.GetAsync(_currentUser.UserId, command.CardId, ct)
                   ?? throw new NotFoundException(BankCardMapping.NotFoundMessage);

        var s = command.Settings;
        card.ChangeSettings(s.Contactless, s.OnlineUse, s.DailyLimit, s.MonthlyLimit);
        await _cards.UpdateAsync(card, ct);

        return BankCardMapping.ToResponse(card);
    }
}

internal static class BankCardMapping
{
    public const string NotFoundMessage = "Kart bulunamadı.";

    public static CardSettingsResponse ToResponse(BankCard card)
        => new(card.Id, card.Status.ToString(), card.Contactless, card.OnlineUse, card.DailyLimit, card.MonthlyLimit);
}
