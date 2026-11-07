using Bank.Contracts.Auth;

namespace Bank.Application.Abstractions.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct = default);
}
