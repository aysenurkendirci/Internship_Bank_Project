using Bank.Application.Common;
using Bank.Contracts.Finance;
using Bank.Domain.Cards;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Features.CreditCards;

internal static class CreditCardMapping
{
    public const string NotFoundMessage = "Kart bulunamadı.";

    public static CreditCard ToDomain(long id, CreditCardRequest request)
        => new(
            id,
            request.Name,
            new Money(request.LimitAmount, Currency.FromCode(request.CurrencyCode)),
            request.StatementDay,
            request.PaymentDueDays);

    public static CreditCardResponse ToResponse(Versioned<CreditCard> stored)
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
