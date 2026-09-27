using Bank.Application.Abstractions.Data;
using Bank.Application.Abstractions.Repositories;
using Bank.Domain.Exceptions;
using MediatR;

namespace Bank.Application.Features.Cards.Commands.UpdateSettings;

public sealed class UpdateCardSettingsCommandHandler : IRequestHandler<UpdateCardSettingsCommand, bool>
{
    private readonly ICardRepository _cardRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCardSettingsCommandHandler(ICardRepository cardRepository, IUnitOfWork unitOfWork)
    {
        _cardRepository = cardRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateCardSettingsCommand request, CancellationToken cancellationToken)
    {
        // 1. Kartı bağlı olduğu hesap bilgisiyle beraber getir
        var card = await _cardRepository.GetByIdWithAccountAsync(request.CardId, cancellationToken);

        if (card is null)
            throw new DomainException("Kart bulunamadı.");

        // 2. Güvenlik Kontrolü: Bu kart, isteği atan kullanıcıya mı ait?
        if (card.Account.UserId != request.UserId)
            throw new DomainException("Bu kart üzerinde işlem yapma yetkiniz yok.");

        // 3. Domain kurallarını (metotlarını) çağırarak güncellemeleri yap
        card.ToggleContactless(request.IsContactlessEnabled);
        card.ToggleOnline(request.IsOnlineEnabled);

        // 4. Veritabanına kaydet
        await _cardRepository.UpdateAsync(card, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
