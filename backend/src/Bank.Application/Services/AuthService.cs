using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Application.Common.Exceptions;
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

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var existing = await _repo.GetUserByTcOrDefaultAsync(req.TcNo, ct);
        if (existing is not null)
            throw new ConflictException("Bu TC kimlik numarası ile kayıtlı bir kullanıcı var.");

        var passwordHash = _hasher.Hash(req.Password);
        var user = await _repo.CreateUserAsync(req, passwordHash, ct);

        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var user = await _repo.GetUserByTcOrDefaultAsync(req.TcNo, ct)
                   ?? throw new AuthenticationFailedException();

        var credentials = await _repo.GetCredentialsAsync(user.UserId, ct);
        if (!_hasher.Verify(req.Password, credentials.PasswordHash))
            throw new AuthenticationFailedException();

        await _repo.UpdateLastLoginAsync(user.UserId, ct);

        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(UserRow user)
        => new(user.UserId, $"{user.FirstName} {user.LastName}", _token.Generate(user.UserId, user.Email));
}
