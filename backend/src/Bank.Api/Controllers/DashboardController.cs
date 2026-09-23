using Bank.Api.Security;
using Bank.Application.Abstractions.Security;
using Bank.Application.Features.Dashboard.Queries.GetDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 🔒 Sadece Token'ı olan (Giriş yapmış) kullanıcılar girebilir.
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser; // Kendi yazdığımız "Kimlik" servisi

    public DashboardController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        // 1. JWT Token'ın içinden giriş yapmış kullanıcının ID'sini okuyoruz.
        long userId = _currentUser.UserId;

        // 2. Kuryeye "Bu ID'li kullanıcının Dashboard bilgilerini getir" diyoruz.
        var query = new GetDashboardQuery(userId);

        // 3. Postacıya veriyoruz.
        var result = await _sender.Send(query, cancellationToken);

        // 4. Sonucu dönüyoruz.
        return Ok(result);
    }
}
