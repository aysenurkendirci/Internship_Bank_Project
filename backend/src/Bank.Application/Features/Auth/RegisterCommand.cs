using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Features.Auth;

/// <summary>Creates a customer and signs them in.</summary>
public sealed record RegisterCommand(RegisterRequest Registration) : IRequest<AuthResult>;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenGenerator _tokens;
    private readonly RefreshTokenIssuer _refreshTokens;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IAuthRepository repo, IPasswordHasher hasher, ITokenGenerator tokens, RefreshTokenIssuer refreshTokens,
        ILogger<RegisterCommandHandler> logger)
    {
        _repo = repo;
        _hasher = hasher;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
        _logger = logger;
    }

    public async Task<AuthResult> Handle(RegisterCommand command, CancellationToken ct)
    {
        var registration = command.Registration;
        var maskedTc = PersonalData.MaskTcKimlikNo(registration.TcNo);

        if (await _repo.GetUserByTcOrDefaultAsync(registration.TcNo, ct) is not null)
        {
            _logger.LogWarning("Registration refused for {TcNo}: identity number already registered", maskedTc);
            throw new ConflictException("Bu TC kimlik numarası ile kayıtlı bir kullanıcı var.");
        }

        var user = await _repo.CreateUserAsync(
            new NewUser(
                registration.TcNo, registration.FirstName, registration.LastName, registration.Email,
                registration.Phone, registration.Membership, _hasher.Hash(registration.Password)),
            ct);

        _logger.LogInformation("User {UserId} registered with {TcNo}", user.UserId, maskedTc);
        return await AuthResponses.CreateAsync(user.UserId, $"{user.FirstName} {user.LastName}", _tokens, _refreshTokens, ct);
    }
}
