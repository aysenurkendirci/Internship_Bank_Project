using System.Text.RegularExpressions;
using Serilog.Context;

namespace Bank.Api.Logging;

/// <summary>
/// Gives every request an id that appears in each log line it produces, in the response headers and in error
/// responses. A customer reporting "the transfer failed" can quote the id, and support finds the exact log lines.
/// </summary>
/// <remarks>
/// An id sent by the caller (e.g. a gateway or the mobile app) is kept so one id follows the request across
/// systems, but only if it looks like an id: anything else could inject text into the logs.
/// </remarks>
public sealed partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = SafeId().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
            await _next(context);
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{8,64}$")]
    private static partial Regex SafeId();
}
