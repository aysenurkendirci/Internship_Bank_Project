using System.ComponentModel.DataAnnotations;

namespace Bank.Contracts.Cards;

public sealed record CardSettingsRequest(
    bool Contactless,
    bool OnlineUse,

    [Range(typeof(decimal), "0", "10000000",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Günlük limit 0 ile 10.000.000 arasında olmalıdır.")]
    decimal DailyLimit,

    [Range(typeof(decimal), "0", "10000000",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Aylık limit 0 ile 10.000.000 arasında olmalıdır.")]
    decimal MonthlyLimit
);

public sealed record CardSettingsResponse(
    long Id,
    string Status,
    bool Contactless,
    bool OnlineUse,
    decimal DailyLimit,
    decimal MonthlyLimit
);
