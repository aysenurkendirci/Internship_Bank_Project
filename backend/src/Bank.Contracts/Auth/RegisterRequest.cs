using System.ComponentModel.DataAnnotations;
using Bank.Contracts.Validation;

namespace Bank.Contracts.Auth;

public sealed record RegisterRequest(
    [Required(ErrorMessage = "TC kimlik numarası zorunludur.")]
    [TcKimlikNo]
    string TcNo,

    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad 2-100 karakter olmalıdır.")]
    string FirstName,

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Soyad 2-100 karakter olmalıdır.")]
    string LastName,

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [StringLength(256)]
    string Email,

    [Required(ErrorMessage = "Telefon zorunludur.")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [StringLength(20)]
    string Phone,

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    string Password,

    [Required(ErrorMessage = "Üyelik tipi zorunludur.")]
    [AllowedValues("Personal", "Corporate", ErrorMessage = "Üyelik tipi Personal veya Corporate olmalıdır.")]
    string Membership
);
