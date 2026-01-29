using Bank.Infrastructure.Oracle;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly OracleExecutor _db;

    public HealthController(OracleExecutor db)
    {
        _db = db;
    }

    [HttpGet("oracle")]
    public async Task<IActionResult> Oracle()
    {
        await _db.TestConnectionAsync();
        return Ok(new { status = "Oracle connection OK" });
    }
}
