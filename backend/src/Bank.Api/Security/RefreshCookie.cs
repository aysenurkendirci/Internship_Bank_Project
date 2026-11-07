using Bank.Application.Features.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Security;

/// <summary>
/// The refresh token lives in a cookie script cannot read (HttpOnly), is only sent over HTTPS, never on
/// cross-site requests (SameSite=Strict) and only to the two endpoints that need it (Path).
/// </summary>
internal static class RefreshCookie
{
    public const string Name = "refresh_token";
    public const string Path = "/api/auth";

    // A browser adds cookies to cross-site requests on its own, but cannot add a custom header without a
    // CORS preflight, which only our own web origin passes. Requiring the header closes the gap SameSite leaves
    // in older browsers.
    public const string RequiredHeader = "X-Requested-By";
    public const string RequiredHeaderValue = "bank-web";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static bool IsFromTheApp(HttpRequest request)
        => request.Headers[RequiredHeader] == RequiredHeaderValue;

    public static ObjectResult Forbidden(ControllerBase controller)
        => controller.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "İstek reddedildi",
            detail: $"{RequiredHeader} başlığı eksik veya geçersiz.");

    public static void Write(HttpResponse response, IssuedRefreshToken token)
        => response.Cookies.Append(Name, token.Value, Options(token.ExpiresAt));

    public static void Clear(HttpResponse response)
        => response.Cookies.Delete(Name, Options(expires: null));

    private static CookieOptions Options(DateTime? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires is null ? null : new DateTimeOffset(DateTime.SpecifyKind(expires.Value, DateTimeKind.Utc))
    };
}
