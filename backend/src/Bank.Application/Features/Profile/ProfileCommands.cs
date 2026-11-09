using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common;
using Bank.Application.Common.Exceptions;
using Bank.Application.Features.Auth;
using Bank.Contracts.Account;
using Bank.Contracts.Auth;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Features.Profile;

/// <summary>The signed-in customer's own details.</summary>
public sealed record GetProfileQuery : IRequest<ProfileResponse>;

public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileResponse>
{
    private readonly IAuthRepository _users;
    private readonly ICurrentUser _currentUser;

    public GetProfileQueryHandler(IAuthRepository users, ICurrentUser currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<ProfileResponse> Handle(GetProfileQuery query, CancellationToken ct)
        => ProfileMapping.ToResponse(await _users.GetProfileAsync(_currentUser.UserId, ct));
}

/// <summary>Changes the e-mail address and phone number.</summary>
public sealed record UpdateProfileCommand(UpdateProfileRequest Profile) : IRequest<ProfileResponse>;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, ProfileResponse>
{
    private readonly IAuthRepository _users;
    private readonly ICurrentUser _currentUser;

    public UpdateProfileCommandHandler(IAuthRepository users, ICurrentUser currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<ProfileResponse> Handle(UpdateProfileCommand command, CancellationToken ct)
    {
        await _users.UpdateContactAsync(_currentUser.UserId, command.Profile.Email.Trim(), command.Profile.Phone.Trim(), ct);
        return ProfileMapping.ToResponse(await _users.GetProfileAsync(_currentUser.UserId, ct));
    }
}

/// <summary>
/// Replaces the password. Every other session ends, in case the change was prompted by a stolen password; the
/// session that made the change gets a new refresh token and carries on.
/// </summary>
public sealed record ChangePasswordCommand(ChangePasswordRequest Change) : IRequest<AuthResult>;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(c => c.Change.NewPassword)
            .NotEqual(c => c.Change.CurrentPassword).WithMessage("Yeni şifre mevcut şifreyle aynı olamaz.")
            .OverridePropertyName("newPassword");
    }
}

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, AuthResult>
{
    private readonly IAuthRepository _users;
    private readonly IRefreshTokenRepository _refreshTokenStore;
    private readonly RefreshTokenIssuer _refreshTokens;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenGenerator _accessTokens;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IAuthRepository users,
        IRefreshTokenRepository refreshTokenStore,
        RefreshTokenIssuer refreshTokens,
        IPasswordHasher hasher,
        ITokenGenerator accessTokens,
        ICurrentUser currentUser,
        TimeProvider time,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _users = users;
        _refreshTokenStore = refreshTokenStore;
        _refreshTokens = refreshTokens;
        _hasher = hasher;
        _accessTokens = accessTokens;
        _currentUser = currentUser;
        _time = time;
        _logger = logger;
    }

    public async Task<AuthResult> Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        var stored = await _users.GetCredentialsAsync(userId, ct);
        if (!_hasher.Verify(command.Change.CurrentPassword, stored.PasswordHash))
        {
            _logger.LogWarning("Password change refused for user {UserId}: wrong current password", userId);
            throw new BadRequestException("Mevcut şifre doğrulanamadı.");
        }

        await _users.UpdatePasswordAsync(userId, _hasher.Hash(command.Change.NewPassword), ct);
        await _refreshTokenStore.RevokeAllAsync(userId, _time.GetUtcNow().UtcDateTime, ct);

        _logger.LogInformation("User {UserId} changed their password", userId);

        var profile = await _users.GetProfileAsync(userId, ct);
        return await AuthResponses.CreateAsync(userId, $"{profile.FirstName} {profile.LastName}", _accessTokens, _refreshTokens, ct);
    }
}

internal static class ProfileMapping
{
    public static ProfileResponse ToResponse(UserProfileRow profile)
        => new(
            profile.FirstName,
            profile.LastName,
            PersonalData.MaskTcKimlikNo(profile.TcNo),
            profile.Email,
            profile.Phone,
            profile.Membership,
            profile.CreatedAt,
            profile.LastLoginAt);
}
