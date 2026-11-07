using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bank.Api.Security;

/// <summary>
/// Caps login and register attempts per client address. Without it, the login endpoint lets anyone try
/// passwords (or lists of identity numbers) as fast as the network allows.
/// </summary>
/// <remarks>
/// Partitioned by remote IP. Behind a reverse proxy every request would share the proxy's address, so a
/// deployment must enable forwarded headers before this partitioning is meaningful.
/// </remarks>
public static class AuthRateLimiting
{
    public const string Policy = "auth";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
        var window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60));

        return services.AddRateLimiter(options =>
        {
            options.AddPolicy(Policy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window, QueueLimit = 0 }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = ((int)window.TotalSeconds).ToString();
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Çok fazla deneme",
                        Detail = "Kısa sürede çok fazla deneme yapıldı. Lütfen biraz bekleyip tekrar deneyin."
                    },
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: ct);
            };
        });
    }
}
