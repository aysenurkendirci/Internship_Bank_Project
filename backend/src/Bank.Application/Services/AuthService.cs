using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Application.Common;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Auth;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Services;

/// <remarks>
/// Every sign-in and sign-up attempt is logged as a security event so brute force and credential stuffing
/// can be spotted. The client always gets the same "wrong identity number or password" answer, but the log
/// keeps the real reason. Identity numbers are masked before they reach the log.
/// </remarks>
public sealed class AuthService : IAuthService
{
    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenGenerator _token;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IAuthRepository repo, IPasswordHasher hasher, ITokenGenerator token, ILogger<AuthService> logger)
    {
        _repo = repo;
        _hasher = hasher;
        _token = token;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var maskedTc = PersonalData.MaskTcKimlikNo(req.TcNo);

        var existing = await _repo.GetUserByTcOrDefaultAsync(req.TcNo, ct);
        if (existing is not null)
        {
            _logger.LogWarning("Registration refused for {TcNo}: identity number already registered", maskedTc);
            throw new ConflictException("Bu TC kimlik numarası ile kayıtlı bir kullanıcı var.");
        }

        var passwordHash = _hasher.Hash(req.Password);
        var user = await _repo.CreateUserAsync(req, passwordHash, ct);

        _logger.LogInformation("User {UserId} registered with {TcNo}", user.UserId, maskedTc);
        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var maskedTc = PersonalData.MaskTcKimlikNo(req.TcNo);

        var user = await _repo.GetUserByTcOrDefaultAsync(req.TcNo, ct);
        if (user is null)
        {
            _logger.LogWarning("Login failed for {TcNo}: unknown identity number", maskedTc);
            throw new AuthenticationFailedException();
        }

        var credentials = await _repo.GetCredentialsAsync(user.UserId, ct);
        if (!_hasher.Verify(req.Password, credentials.PasswordHash))
        {
            _logger.LogWarning("Login failed for user {UserId} ({TcNo}): wrong password", user.UserId, maskedTc);
            throw new AuthenticationFailedException();
        }

        await _repo.UpdateLastLoginAsync(user.UserId, ct);

        _logger.LogInformation("User {UserId} logged in", user.UserId);
        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(UserRow user)
        => new(user.UserId, $"{user.FirstName} {user.LastName}", _token.Generate(user.UserId, user.Email));
}
