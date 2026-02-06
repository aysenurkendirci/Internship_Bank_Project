using Bank.Contracts.Transfers;

namespace Bank.Application.Abstractions.Repositories;

public interface ITransfersRepository
{
    Task<TransferResponse> CreateAsync(CreateTransferRequest req, CancellationToken ct = default);
}
