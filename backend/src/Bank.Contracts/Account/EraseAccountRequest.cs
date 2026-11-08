using System.ComponentModel.DataAnnotations;

namespace Bank.Contracts.Account;

/// <remarks>
/// Erasure cannot be undone, so the password is asked again even though the caller is signed in: a token left
/// in an open browser should not be enough to wipe an account.
/// </remarks>
public sealed record EraseAccountRequest(
    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(128, ErrorMessage = "Şifre en fazla 128 karakter olabilir.")]
    string Password
);
