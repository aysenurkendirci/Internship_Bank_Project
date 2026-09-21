using MediatR;
using Bank.Contracts.Auth; // Dönüş tipi olarak LoginResponse kullanmak için

namespace Bank.Application.Features.Auth.Commands.Login;

// Command: Sadece veri taşır. CQRS'de "Ne yapılmak isteniyor?" sorusunun cevabıdır.
// IRequest<LoginResponse> => Bu işlem sonucunda dışarıya LoginResponse dönülecek demek.
public sealed record LoginCommand(
    string TcNo, 
    string Password
) : IRequest<AuthResponse>;
