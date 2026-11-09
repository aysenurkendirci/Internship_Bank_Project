using System.Security.Claims;
using Bank.Application.Abstractions.Security;

namespace Bank.Api.Security;

/// <summary>Reads the signed-in user from the validated token, so handlers never touch HttpContext.</summary>
public sealed class CurrentUser : ICurrentUser
{
    public long UserId { get; }
    public bool IsAuthenticated { get; }

    public CurrentUser(IHttpContextAccessor accessor)
    {
        var http = accessor.HttpContext;
        var user = http?.User;

        // 0 when there is no token; every endpoint that reads it requires authentication anyway.
        UserId = TryGetLong(user, ClaimTypes.NameIdentifier) ?? 0;
        IsAuthenticated = user?.Identity?.IsAuthenticated ?? false;
    }

    private static long? TryGetLong(ClaimsPrincipal? user, string claimType)
    {
        var raw = user?.FindFirstValue(claimType);
        return long.TryParse(raw, out var v) ? v : null;
    }
}