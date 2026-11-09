using System.ComponentModel.DataAnnotations;

namespace Bank.Contracts.Account;

/// <summary>What the profile screen shows. The identity number arrives masked; only the data export has it in full.</summary>
public sealed record ProfileResponse(
    string FirstName,
    string LastName,
    string MaskedTcNo,
    string Email,
    string Phone,
    string Membership,
    DateTime RegisteredAt,
    DateTime? LastLoginAt
);

/// <summary>The details a customer may change themselves. Name and identity number are verified facts and stay fixed.</summary>
public sealed record UpdateProfileRequest(
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [StringLength(256)]
    string Email,

    [Required(ErrorMessage = "Telefon zorunludur.")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [StringLength(20)]
    string Phone
);

public sealed record ChangePasswordRequest(
    [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
    [StringLength(128)]
    string CurrentPassword,

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    string NewPassword
);
