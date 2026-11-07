using Bank.Api.Security;
using Bank.Application.Features.Auth;
using Bank.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting(AuthRateLimiting.Policy)]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new LoginCommand(request), ct));

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new RegisterCommand(request), ct));
}
