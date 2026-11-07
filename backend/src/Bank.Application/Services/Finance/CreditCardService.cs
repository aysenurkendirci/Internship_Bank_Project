using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Application.Common;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Finance;
using Bank.Domain.Cards;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Services.Finance;

public sealed class CreditCardService : ICreditCardService
{
    private const string NotFoundMessage = "Kart bulunamadı.";

    private readonly ICreditCardRepository _repo;
    private readonly ICurrentUser _currentUser;

    public CreditCardService(ICreditCardRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CreditCardResponse>> ListAsync(CancellationToken ct = default)
    {
        var cards = await _repo.ListAsync(_currentUser.UserId, ct);
        return cards.Select(ToResponse).ToList();
    }

    public async Task<CreditCardResponse> GetAsync(long id, CancellationToken ct = default)
    {
        var card = await _repo.GetAsync(_currentUser.UserId, id, ct)
                   ?? throw new NotFoundException(NotFoundMessage);
        return ToResponse(card);
    }

    public async Task<CreditCardResponse> CreateAsync(CreditCardRequest request, CancellationToken ct = default)
    {
        var created = await _repo.AddAsync(_currentUser.UserId, ToDomain(id: 0, request), ct);
        return ToResponse(created);
    }

    public async Task<CreditCardResponse> UpdateAsync(
        long id, CreditCardRequest request, string version, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var current = await _repo.GetAsync(userId, id, ct)
                      ?? throw new NotFoundException(NotFoundMessage);

        // Installments and card-paid items are stored in the card's currency;
        // switching it underneath them would make their schedules invalid.
        var currencyChanged = current.Value.Limit.Currency.Code != request.CurrencyCode;
        if (currencyChanged && await _repo.IsInUseAsync(userId, id, ct))
            throw new ConflictException("Taksit veya düzenli ödemesi olan kartın para birimi değiştirilemez.");

        var updated = ToDomain(id, request);
        var newVersion = await _repo.UpdateAsync(userId, updated, RowVersion.Decode(version), ct);

        if (newVersion is null)
            throw await _repo.ExistsAsync(userId, id, ct)
                ? StaleVersion()
                : new NotFoundException(NotFoundMessage);

        return ToResponse(new Versioned<CreditCard>(updated, newVersion));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (await _repo.SoftDeleteIfUnusedAsync(userId, id, ct))
            return;

        throw await _repo.ExistsAsync(userId, id, ct)
            ? new ConflictException("Bu karta bağlı taksit veya düzenli ödeme var. Önce onları kaldırın.")
            : new NotFoundException(NotFoundMessage);
    }

    private static ConflictException StaleVersion()
        => new("Kart siz düzenlerken değiştirildi. Güncel hâlini yükleyip tekrar deneyin.");

    private static CreditCard ToDomain(long id, CreditCardRequest request)
        => new(
            id,
            request.Name,
            new Money(request.LimitAmount, Currency.FromCode(request.CurrencyCode)),
            request.StatementDay,
            request.PaymentDueDays);

    private static CreditCardResponse ToResponse(Versioned<CreditCard> stored)
    {
        var card = stored.Value;
        return new CreditCardResponse(
            card.Id,
            card.Name,
            card.Limit.Amount,
            card.Limit.Currency.Code,
            card.StatementDay,
            card.PaymentDueDays,
            RowVersion.Encode(stored.Version));
    }
}
