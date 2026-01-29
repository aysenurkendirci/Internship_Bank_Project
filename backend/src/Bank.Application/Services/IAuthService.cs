using Bank.Contracts.Auth;

namespace Bank.Application.Abstractions.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest req);
    Task<AuthResponse> RegisterAsync(RegisterRequest req);
}
