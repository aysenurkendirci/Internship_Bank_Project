using MediatR;

namespace Bank.Application.Features.Transactions.Commands.TransferMoney;

public sealed record TransferMoneyCommand(
    long SenderUserId, // Güvenlik için token'dan gelecek
    long SenderAccountId,
    long ReceiverAccountId,
    decimal Amount,
    string Description
) : IRequest<bool>;
