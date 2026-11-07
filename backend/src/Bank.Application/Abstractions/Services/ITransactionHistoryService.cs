using Bank.Contracts.Finance;

namespace Bank.Application.Abstractions.Services;

public interface ITransactionHistoryService
{
    Task<ImportResultResponse> ImportCsvAsync(string? fileName, byte[] content, CancellationToken ct = default);

    Task<PagedResponse<HistoryTransactionResponse>> ListAsync(
        DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default);
}
