namespace Bank.Api.Security;

/// <summary>
/// Baseline response headers for an API that returns personal financial data.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><c>Cache-Control: no-store</c> — balances and transactions must not linger in browser or proxy caches,
///   for example on a shared computer after the user logs out.</item>
///   <item><c>X-Content-Type-Options: nosniff</c> — browsers must not guess a different type for a JSON response.</item>
///   <item><c>X-Frame-Options: DENY</c> and a strict CSP — nothing the API returns is meant to be framed or to run
///   scripts. Swagger UI, available in development only, needs scripts and is left out of the CSP.</item>
///   <item><c>Referrer-Policy: no-referrer</c> — URLs can carry ids and should not leak to other sites.</item>
/// </list>
/// </remarks>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.CacheControl = "no-store";
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";

            if (!context.Request.Path.StartsWithSegments("/swagger"))
                headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

            return Task.CompletedTask;
        });

        return _next(context);
    }
}
