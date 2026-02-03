using System.Security.Claims;
using Bank.Application.Abstractions.Security;

namespace Bank.Api.Security;

public sealed class CurrentUser : ICurrentUser
{
    public long UserId { get; }
    public string Email { get; }

    public bool IsAuthenticated { get; }

    public CurrentUser(IHttpContextAccessor accessor)
    {
        var http = accessor.HttpContext;
        var user = http?.User;

        // Token yoksa 0
        UserId = TryGetLong(user, ClaimTypes.NameIdentifier) ?? 0;
        Email = user?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        IsAuthenticated = user?.Identity?.IsAuthenticated ?? false;
    }

    private static long? TryGetLong(ClaimsPrincipal? user, string claimType)
    {
        var raw = user?.FindFirstValue(claimType);
        return long.TryParse(raw, out var v) ? v : null;
    }
}