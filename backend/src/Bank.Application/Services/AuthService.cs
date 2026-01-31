using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Contracts.Auth;

namespace Bank.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenGenerator _token;

    public AuthService(IAuthRepository repo, IPasswordHasher hasher, ITokenGenerator token)
    {
        _repo = repo;
        _hasher = hasher;
        _token = token;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        // ✅ 500 yerine kontrollü hata: "zaten var"
        var existing = await _repo.GetUserByTcOrDefaultAsync(req.TcNo);
        if (existing is not null)
            throw new InvalidOperationException("Bu TC ile kayıt zaten var.");

        var passwordHash = _hasher.Hash(req.Password);
        var user = await _repo.CreateUserAsync(req, passwordHash);

        var jwt = _token.Generate(user.UserId, user.Email);
        return new AuthResponse(user.UserId, $"{user.FirstName} {user.LastName}", jwt);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _repo.GetUserByTcAsync(req.TcNo);

        var cred = await _repo.GetCredentialsAsync(user.UserId);

        if (!_hasher.Verify(req.Password, cred.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        await _repo.UpdateLastLoginAsync(user.UserId);

        var jwt = _token.Generate(user.UserId, user.Email);
        return new AuthResponse(user.UserId, $"{user.FirstName} {user.LastName}", jwt);
    }
}
