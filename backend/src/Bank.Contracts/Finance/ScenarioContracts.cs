using System.ComponentModel.DataAnnotations;

namespace Bank.Contracts.Finance;

/// <remarks>Nothing is saved: the purchase only exists for the duration of the calculation.</remarks>
public sealed record InstallmentScenarioRequest(
    [Required(ErrorMessage = "Kart seçimi zorunludur.")]
    long? CreditCardId,

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(200, ErrorMessage = "Açıklama en fazla 200 karakter olabilir.")]
    string Description,

    [Required(ErrorMessage = "Alışveriş tarihi zorunludur.")]
    DateOnly? PurchaseDate,

    // Mirrors ScenarioComparer.MaxOptions.
    [Required(ErrorMessage = "En az bir seçenek gereklidir.")]
    [Length(1, 6, ErrorMessage = "1 ile 6 arasında seçenek karşılaştırılabilir.")]
    IReadOnlyList<PurchaseOptionRequest> Options,

    /// <summary>Minimum horizon; it is extended automatically to cover the longest option.</summary>
    [Range(1, 12, ErrorMessage = "Süre 1 ile 12 ay arasında olmalıdır.")]
    int Months = 3
);

public sealed record PurchaseOptionRequest(
    [Range(1, 36, ErrorMessage = "Taksit sayısı 1 ile 36 arasında olmalıdır.")]
    int InstallmentCount,

    [Range(typeof(decimal), "0.01", "999999999",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
    decimal TotalAmount
);

/// <param name="Baseline">The same period without the purchase.</param>
public sealed record ScenarioComparisonResponse(
    DateOnly From,
    DateOnly To,
    ScenarioMetricsResponse Baseline,
    IReadOnlyList<ScenarioOptionResponse> Options
);

public sealed record ScenarioOptionResponse(
    int InstallmentCount,
    decimal TotalAmount,
    IReadOnlyList<InstallmentResponse> Installments,
    ScenarioMetricsResponse Metrics
);

public sealed record ScenarioMetricsResponse(
    decimal LowestBalance,
    DateOnly? LowestBalanceDate,
    int MonthsBelowZero,
    int DeficitMonths,
    decimal? PeakBurdenRatio,
    decimal PeakMonthlyOutflow,
    decimal ClosingBalance,
    IReadOnlyList<ProjectionMonthResponse> Months
);
