using System.ComponentModel.DataAnnotations;
using Bank.Application.Abstractions.Services;
using Bank.Contracts.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/transactions")]
[Authorize]
public sealed class TransactionHistoryController : ControllerBase
{
    private const int MaxFileBytes = 2 * 1024 * 1024;

    private readonly ITransactionHistoryService _history;

    public TransactionHistoryController(ITransactionHistoryService history) => _history = history;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<HistoryTransactionResponse>>> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery, Range(1, int.MaxValue, ErrorMessage = "Sayfa 1 veya daha büyük olmalıdır.")] int page = 1,
        [FromQuery, Range(1, 200, ErrorMessage = "Sayfa boyutu 1 ile 200 arasında olmalıdır.")] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await _history.ListAsync(from, to, page, pageSize, ct));

    /// <summary>
    /// Imports a bank statement CSV. Lines already imported are skipped and counted as duplicates,
    /// so uploading the same or an overlapping statement again is safe.
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileBytes + 64 * 1024)]
    public async Task<ActionResult<ImportResultResponse>> Import(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Geçersiz dosya", detail: "Dosya boş.");
        if (file.Length > MaxFileBytes)
            return Problem(statusCode: StatusCodes.Status413PayloadTooLarge, title: "Dosya çok büyük",
                           detail: "Dosya en fazla 2 MB olabilir.");

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);

        return Ok(await _history.ImportCsvAsync(Path.GetFileName(file.FileName), buffer.ToArray(), ct));
    }
}
