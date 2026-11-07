using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Contracts.Auth;

namespace Bank.Application.Features.Auth;

internal static class AuthResponses
{
    public static AuthResponse For(UserRow user, ITokenGenerator tokens)
        => new(user.UserId, $"{user.FirstName} {user.LastName}", tokens.Generate(user.UserId));
}
