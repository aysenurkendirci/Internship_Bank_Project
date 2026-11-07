using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Cards;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Features.Cards;

/// <summary>Temporarily blocks a card or opens it again, e.g. while the customer looks for a misplaced card.</summary>
public sealed record SetCardBlockedCommand(long CardId, bool Blocked) : IRequest<CardSettingsResponse>;

public sealed class SetCardBlockedCommandHandler : IRequestHandler<SetCardBlockedCommand, CardSettingsResponse>
{
    private readonly IBankCardRepository _cards;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SetCardBlockedCommandHandler> _logger;

    public SetCardBlockedCommandHandler(IBankCardRepository cards, ICurrentUser currentUser, ILogger<SetCardBlockedCommandHandler> logger)
    {
        _cards = cards;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<CardSettingsResponse> Handle(SetCardBlockedCommand command, CancellationToken ct)
    {
        var card = await _cards.GetAsync(_currentUser.UserId, command.CardId, ct)
                   ?? throw new NotFoundException(BankCardMapping.NotFoundMessage);

        if (command.Blocked)
            card.Block();
        else
            card.Unblock();

        await _cards.UpdateAsync(card, ct);

        // A card being blocked or reopened is a security event support staff may need to trace.
        _logger.LogInformation("User {UserId} {Action} card {CardId}",
            _currentUser.UserId, command.Blocked ? "blocked" : "unblocked", card.Id);

        return BankCardMapping.ToResponse(card);
    }
}
