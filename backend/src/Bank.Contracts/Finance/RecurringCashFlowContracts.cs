using System.ComponentModel.DataAnnotations;

namespace Bank.Contracts.Finance;

public sealed record RecurringCashFlowRequest(
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
    string Name,

    [Required(ErrorMessage = "Tür zorunludur.")]
    [AllowedValues("Income", "Expense", "Subscription", ErrorMessage = "Tür Income, Expense veya Subscription olmalıdır.")]
    string Kind,

    [Required(ErrorMessage = "Kategori zorunludur.")]
    [StringLength(50, ErrorMessage = "Kategori en fazla 50 karakter olabilir.")]
    string Category,

    [Range(typeof(decimal), "0.01", "999999999",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
    decimal Amount,

    [Required(ErrorMessage = "Para birimi zorunludur.")]
    [AllowedValues("TRY", "USD", "EUR", ErrorMessage = "Para birimi TRY, USD veya EUR olmalıdır.")]
    string CurrencyCode,

    [Required(ErrorMessage = "Tekrar sıklığı zorunludur.")]
    [AllowedValues("Weekly", "Monthly", "Yearly", ErrorMessage = "Sıklık Weekly, Monthly veya Yearly olmalıdır.")]
    string Frequency,

    [Range(1, 52, ErrorMessage = "Tekrar aralığı 1 ile 52 arasında olmalıdır.")]
    int Interval,

    // Nullable so that a missing date fails [Required] instead of silently becoming 0001-01-01.
    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    DateOnly? StartDate,

    DateOnly? EndDate,

    long? CreditCardId
);

public sealed record RecurringCashFlowResponse(
    long Id,
    string Name,
    string Kind,
    string Category,
    decimal Amount,
    string CurrencyCode,
    string Frequency,
    int Interval,
    DateOnly StartDate,
    DateOnly? EndDate,
    long? CreditCardId,
    string Version
);
