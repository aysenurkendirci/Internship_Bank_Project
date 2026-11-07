using Bank.Application.Features.TransactionHistory;
using Bank.Contracts.Finance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/transactions")]
[Authorize]
public sealed class TransactionHistoryController : ControllerBase
{
    private const int MaxFileBytes = 2 * 1024 * 1024;

    private readonly ISender _sender;

    public TransactionHistoryController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<HistoryTransactionResponse>>> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new ListTransactionsQuery(from, to, page, pageSize), ct));

    /// <summary>Generates a reproducible demo history for the last <paramref name="months"/> months.</summary>
    [HttpPost("demo")]
    public async Task<ActionResult<ImportResultResponse>> GenerateDemo(
        [FromQuery] int months = 12,
        [FromQuery] int seed = 42,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new GenerateDemoHistoryCommand(months, seed), ct));

    /// <summary>Imports a bank statement CSV; lines imported before are skipped and counted as duplicates.</summary>
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

        return Ok(await _sender.Send(new ImportStatementCommand(Path.GetFileName(file.FileName), buffer.ToArray()), ct));
    }
}
