using System.ComponentModel.DataAnnotations;

namespace Bank.Contracts.Auth;

// Only presence is checked here: format errors on login would tell an attacker which part was wrong.
public sealed record LoginRequest(
    [Required(ErrorMessage = "TC kimlik numarası zorunludur.")] string TcNo,
    [Required(ErrorMessage = "Şifre zorunludur.")] string Password
);
