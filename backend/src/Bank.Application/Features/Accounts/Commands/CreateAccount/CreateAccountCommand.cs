using MediatR;

namespace Bank.Application.Features.Accounts.Commands.CreateAccount;

public sealed record CreateAccountCommand(
    long UserId, 
    int Type,
    string Currency
) : IRequest<long>;
