using System.ComponentModel.DataAnnotations;

namespace Bank.Contracts.Finance;

/// <remarks>The plan is billed in the card's currency, so the request carries no currency of its own.</remarks>
public sealed record InstallmentPlanRequest(
    [Required(ErrorMessage = "Kart seçimi zorunludur.")]
    long? CreditCardId,

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(200, ErrorMessage = "Açıklama en fazla 200 karakter olabilir.")]
    string Description,

    [Range(typeof(decimal), "0.01", "999999999",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
    decimal TotalAmount,

    // Mirrors InstallmentPlan.MaxInstallmentCount; Contracts does not reference the domain.
    [Range(1, 36, ErrorMessage = "Taksit sayısı 1 ile 36 arasında olmalıdır.")]
    int InstallmentCount,

    [Required(ErrorMessage = "Alışveriş tarihi zorunludur.")]
    DateOnly? PurchaseDate
);

public sealed record InstallmentPlanResponse(
    long Id,
    long CreditCardId,
    string Description,
    decimal TotalAmount,
    string CurrencyCode,
    int InstallmentCount,
    DateOnly PurchaseDate,
    IReadOnlyList<InstallmentResponse> Installments
);

public sealed record InstallmentResponse(int Number, DateOnly StatementDate, DateOnly DueDate, decimal Amount);
