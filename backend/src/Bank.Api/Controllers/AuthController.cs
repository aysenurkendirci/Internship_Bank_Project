using Bank.Application.Features.Auth.Commands.Login;
using Bank.Application.Features.Auth.Commands.Register;
using Bank.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        // Gelen JSON isteğini (LoginRequest), sistemimizin anladığı kuryeye (LoginCommand) çeviriyoruz.
        var command = new LoginCommand(request.TcNo, request.Password);
        
        // Kuryeyi MediatR postacısına veriyoruz. O gidip LoginCommandHandler'ı bulup çalıştıracak.
        var result = await _sender.Send(command, cancellationToken);
        
        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        // Gelen kayıt bilgilerini kuryeye (RegisterCommand) yüklüyoruz.
        var command = new RegisterCommand(
            request.TcNo,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.Password,
            request.Membership
        );

        // Kuryeyi postacıya veriyoruz. O da RegisterCommandHandler'ı çalıştıracak.
        var result = await _sender.Send(command, cancellationToken);
        
        return Ok(result);
    }
}
