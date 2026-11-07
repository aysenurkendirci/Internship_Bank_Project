using System.ComponentModel.DataAnnotations;

namespace Bank.Contracts.Finance;

public sealed record CreditCardRequest(
    [Required(ErrorMessage = "Kart adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Kart adı en fazla 100 karakter olabilir.")]
    string Name,

    // Limits are written with "." and must not be parsed with the server culture (tr-TR uses ",").
    [Range(typeof(decimal), "0.01", "999999999",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Limit sıfırdan büyük olmalıdır.")]
    decimal LimitAmount,

    [Required(ErrorMessage = "Para birimi zorunludur.")]
    [AllowedValues("TRY", "USD", "EUR", ErrorMessage = "Para birimi TRY, USD veya EUR olmalıdır.")]
    string CurrencyCode,

    [Range(1, 31, ErrorMessage = "Hesap kesim günü 1 ile 31 arasında olmalıdır.")]
    int StatementDay,

    [Range(1, 30, ErrorMessage = "Son ödeme süresi 1 ile 30 gün arasında olmalıdır.")]
    int PaymentDueDays
);

public sealed record CreditCardResponse(
    long Id,
    string Name,
    decimal LimitAmount,
    string CurrencyCode,
    int StatementDay,
    int PaymentDueDays,
    string Version
);
