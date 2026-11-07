using Bank.Application.Abstractions.Repositories;
using Bank.Application.Common;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Finance;
using Bank.Domain.CashFlows;
using Bank.Domain.Scheduling;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Features.RecurringCashFlows;

internal static class RecurringCashFlowMapping
{
    public const string NotFoundMessage = "Kayıt bulunamadı.";

    public static RecurringCashFlow ToDomain(long id, RecurringCashFlowRequest request)
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

    public static RecurringCashFlowResponse ToResponse(Versioned<RecurringCashFlow> stored)
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

    /// <summary>
    /// The database rejects a card owned by someone else (composite foreign key), but it cannot see
    /// soft deletes or currencies; those are checked here to give the user a clear message.
    /// </summary>
    public static async Task EnsureCardIsUsableAsync(
        RecurringCashFlow item, long userId, ICreditCardRepository cards, CancellationToken ct)
    {
        if (item.CreditCardId is not { } cardId)
            return;

        var card = await cards.GetAsync(userId, cardId, ct)
                   ?? throw new BadRequestException("Seçilen kart bulunamadı.");

        if (card.Value.Limit.Currency != item.Amount.Currency)
            throw new BadRequestException("Karta bağlı ödemenin para birimi kartın para birimiyle aynı olmalıdır.");
    }
}
