using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Application.Common;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Finance;
using Bank.Domain.CashFlows;
using Bank.Domain.Scheduling;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Services.Finance;

public sealed class RecurringCashFlowService : IRecurringCashFlowService
{
    private const string NotFoundMessage = "Kayıt bulunamadı.";

    private readonly IRecurringCashFlowRepository _repo;
    private readonly ICreditCardRepository _cards;
    private readonly ICurrentUser _currentUser;

    public RecurringCashFlowService(
        IRecurringCashFlowRepository repo, ICreditCardRepository cards, ICurrentUser currentUser)
    {
        _repo = repo;
        _cards = cards;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<RecurringCashFlowResponse>> ListAsync(string? kind, CancellationToken ct = default)
    {
        var filter = kind is null ? (CashFlowKind?)null : Enum.Parse<CashFlowKind>(kind);
        var items = await _repo.ListAsync(_currentUser.UserId, filter, ct);
        return items.Select(ToResponse).ToList();
    }

    public async Task<RecurringCashFlowResponse> GetAsync(long id, CancellationToken ct = default)
    {
        var item = await _repo.GetAsync(_currentUser.UserId, id, ct)
                   ?? throw new NotFoundException(NotFoundMessage);
        return ToResponse(item);
    }

    public async Task<RecurringCashFlowResponse> CreateAsync(RecurringCashFlowRequest request, CancellationToken ct = default)
    {
        var item = ToDomain(id: 0, request);
        await EnsureCardIsUsableAsync(item, ct);

        return ToResponse(await _repo.AddAsync(_currentUser.UserId, item, ct));
    }

    public async Task<RecurringCashFlowResponse> UpdateAsync(
        long id, RecurringCashFlowRequest request, string version, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var item = ToDomain(id, request);
        await EnsureCardIsUsableAsync(item, ct);

        var newVersion = await _repo.UpdateAsync(userId, item, RowVersion.Decode(version), ct);
        if (newVersion is null)
            throw await _repo.ExistsAsync(userId, id, ct)
                ? new ConflictException("Kayıt siz düzenlerken değiştirildi. Güncel hâlini yükleyip tekrar deneyin.")
                : new NotFoundException(NotFoundMessage);

        return ToResponse(new Versioned<RecurringCashFlow>(item, newVersion));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        if (!await _repo.SoftDeleteAsync(_currentUser.UserId, id, ct))
            throw new NotFoundException(NotFoundMessage);
    }

    // The database rejects a card owned by someone else (composite foreign key), but it cannot see
    // soft deletes or currencies; those are checked here to give the user a clear message.
    private async Task EnsureCardIsUsableAsync(RecurringCashFlow item, CancellationToken ct)
    {
        if (item.CreditCardId is not { } cardId)
            return;

        var card = await _cards.GetAsync(_currentUser.UserId, cardId, ct)
                   ?? throw new BadRequestException("Seçilen kart bulunamadı.");

        if (card.Value.Limit.Currency != item.Amount.Currency)
            throw new BadRequestException("Karta bağlı ödemenin para birimi kartın para birimiyle aynı olmalıdır.");
    }

    private static RecurringCashFlow ToDomain(long id, RecurringCashFlowRequest request)
        => new(
            id,
            request.Name,
            Enum.Parse<CashFlowKind>(request.Kind),
            request.Category,
            new Money(request.Amount, Currency.FromCode(request.CurrencyCode)),
            new RecurrenceRule(
                Enum.Parse<Frequency>(request.Frequency),
                request.StartDate!.Value,
                request.Interval,
                request.EndDate),
            request.CreditCardId);

    private static RecurringCashFlowResponse ToResponse(Versioned<RecurringCashFlow> stored)
    {
        var item = stored.Value;
        return new RecurringCashFlowResponse(
            item.Id,
            item.Name,
            item.Kind.ToString(),
            item.Category,
            item.Amount.Amount,
            item.Amount.Currency.Code,
            item.Recurrence.Frequency.ToString(),
            item.Recurrence.Interval,
            item.Recurrence.StartDate,
            item.Recurrence.EndDate,
            item.CreditCardId,
            RowVersion.Encode(stored.Version));
    }
}
