using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Features.Auth;

public sealed record RegisterCommand(RegisterRequest Registration) : IRequest<AuthResponse>;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenGenerator _tokens;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IAuthRepository repo, IPasswordHasher hasher, ITokenGenerator tokens, ILogger<RegisterCommandHandler> logger)
    {
        _repo = repo;
        _hasher = hasher;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(RegisterCommand command, CancellationToken ct)
    {
        var registration = command.Registration;
        var maskedTc = PersonalData.MaskTcKimlikNo(registration.TcNo);

        if (await _repo.GetUserByTcOrDefaultAsync(registration.TcNo, ct) is not null)
        {
            _logger.LogWarning("Registration refused for {TcNo}: identity number already registered", maskedTc);
            throw new ConflictException("Bu TC kimlik numarası ile kayıtlı bir kullanıcı var.");
        }

        var user = await _repo.CreateUserAsync(registration, _hasher.Hash(registration.Password), ct);

        _logger.LogInformation("User {UserId} registered with {TcNo}", user.UserId, maskedTc);
        return AuthResponses.For(user, _tokens);
    }
}
