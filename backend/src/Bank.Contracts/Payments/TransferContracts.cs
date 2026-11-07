using System.ComponentModel.DataAnnotations;

namespace Bank.Contracts.Payments;

/// <remarks>The amount is in the source account's currency; transfers between currencies are not supported.</remarks>
public sealed record TransferRequest(
    [Required(ErrorMessage = "Gönderen hesap zorunludur.")]
    long? SourceAccountId,

    [Required(ErrorMessage = "Alıcı IBAN zorunludur.")]
    [StringLength(40, ErrorMessage = "IBAN en fazla 40 karakter olabilir.")]
    string TargetIban,

    // Checked against the account holder, as FAST and EFT do.
    [Required(ErrorMessage = "Alıcı adı zorunludur.")]
    [StringLength(140, ErrorMessage = "Alıcı adı en fazla 140 karakter olabilir.")]
    string RecipientName,

    [Range(typeof(decimal), "0.01", "999999999",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
    decimal Amount,

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(140, ErrorMessage = "Açıklama en fazla 140 karakter olabilir.")]
    string Description
);

/// <param name="SourceBalanceAfter">The sender's balance right after this transfer.</param>
public sealed record TransferResponse(
    long Id,
    long SourceAccountId,
    string SourceIban,
    string TargetIban,
    string RecipientName,
    decimal Amount,
    string CurrencyCode,
    string Description,
    decimal SourceBalanceAfter,
    DateTime CreatedAt
);
