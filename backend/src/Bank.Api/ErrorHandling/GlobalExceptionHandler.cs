using Bank.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Bank.Api.ErrorHandling;

/// <summary>
/// Translates exceptions into RFC 9457 problem details responses.
/// Only <see cref="AppException"/> messages reach the client; any other exception becomes a
/// generic 500 so stack traces, SQL errors and internal names are never leaked.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Kayıt bulunamadı"),
            ConflictException => (StatusCodes.Status409Conflict, "İşlem mevcut kayıtla çakışıyor"),
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Kimlik doğrulama başarısız"),
            _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Title = title,
                Detail = exception is AppException ? exception.Message : null
            }
        });
    }
}
