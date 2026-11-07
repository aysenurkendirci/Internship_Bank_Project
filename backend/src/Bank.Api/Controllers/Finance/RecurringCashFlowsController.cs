using System.ComponentModel.DataAnnotations;
using Bank.Application.Abstractions.Services;
using Bank.Contracts.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/recurring-cash-flows")]
[Authorize]
public sealed class RecurringCashFlowsController : ControllerBase
{
    private readonly IRecurringCashFlowService _items;

    public RecurringCashFlowsController(IRecurringCashFlowService items) => _items = items;

    /// <param name="kind">Optional filter, e.g. <c>Subscription</c> to review all subscriptions.</param>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RecurringCashFlowResponse>>> List(
        [FromQuery, AllowedValues("Income", "Expense", "Subscription", null, ErrorMessage = "Geçersiz tür filtresi.")]
        string? kind,
        CancellationToken ct)
        => Ok(await _items.ListAsync(kind, ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<RecurringCashFlowResponse>> Get(long id, CancellationToken ct)
        => Ok(await _items.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<RecurringCashFlowResponse>> Create(
        [FromBody] RecurringCashFlowRequest request, CancellationToken ct)
    {
        var created = await _items.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<RecurringCashFlowResponse>> Update(
        long id,
        [FromBody] RecurringCashFlowRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            return Problem(statusCode: StatusCodes.Status428PreconditionRequired,
                           title: "Kayıt sürümü gerekli",
                           detail: "Güncelleme için If-Match başlığında kaydın version değeri gönderilmelidir.");

        return Ok(await _items.UpdateAsync(id, request, ifMatch, ct));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _items.DeleteAsync(id, ct);
        return NoContent();
    }
}
