using Bank.Application.Features.Planning;
using Bank.Contracts.Finance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/scenarios")]
[Authorize]
public sealed class ScenariosController : ControllerBase
{
    private readonly ISender _sender;

    public ScenariosController(ISender sender) => _sender = sender;

    /// <summary>
    /// Compares installment options for a planned card purchase against the user's current outlook.
    /// POST because the scenario is sent in the body; nothing is created or stored.
    /// </summary>
    [HttpPost("installments")]
    public async Task<ActionResult<ScenarioComparisonResponse>> CompareInstallments(
        [FromBody] InstallmentScenarioRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new CompareInstallmentOptionsQuery(request), ct));
}
