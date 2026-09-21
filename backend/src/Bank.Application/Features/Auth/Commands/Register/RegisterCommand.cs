using MediatR;
using Bank.Contracts.Auth;

namespace Bank.Application.Features.Auth.Commands.Register;

// Command: Dışarıdan gelen kayıt bilgilerini taşıyan kurye.
// Bu işlemin sonucunda da giriş yapmış gibi AuthResponse (Token) dönmek mantıklıdır.
public sealed record RegisterCommand(
    string TcNo,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Password,
    string Membership
) : IRequest<AuthResponse>;
