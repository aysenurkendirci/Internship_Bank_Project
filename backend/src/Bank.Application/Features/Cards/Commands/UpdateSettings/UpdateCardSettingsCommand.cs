using MediatR;

namespace Bank.Application.Features.Cards.Commands.UpdateSettings;

public sealed record UpdateCardSettingsCommand(
    long UserId, // Güvenlik için token'dan gelecek
    long CardId,
    bool IsContactlessEnabled,
    bool IsOnlineEnabled
) : IRequest<bool>;
