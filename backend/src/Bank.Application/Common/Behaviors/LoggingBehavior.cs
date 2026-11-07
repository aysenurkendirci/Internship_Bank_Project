using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Common.Behaviors;

/// <summary>
/// Records how long each command and query takes and warns about slow ones.
/// Only the request's type name is logged, never its contents: commands carry identity numbers,
/// passwords and amounts.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan SlowThreshold = TimeSpan.FromMilliseconds(500);

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        var response = await next();
        var elapsed = Stopwatch.GetElapsedTime(started);

        if (elapsed > SlowThreshold)
            _logger.LogWarning("{Request} took {ElapsedMs} ms", typeof(TRequest).Name, (long)elapsed.TotalMilliseconds);
        else
            _logger.LogDebug("{Request} handled in {ElapsedMs} ms", typeof(TRequest).Name, (long)elapsed.TotalMilliseconds);

        return response;
    }
}
