using Bank.Contracts.Finance;

namespace Bank.Application.Abstractions.Services;

public interface ITransactionHistoryService
{
    Task<ImportResultResponse> ImportCsvAsync(string? fileName, byte[] content, CancellationToken ct = default);

    /// <summary>Fills the user's history with a reproducible synthetic profile for the last <paramref name="months"/> months.</summary>
    Task<ImportResultResponse> GenerateDemoHistoryAsync(int months, int seed, CancellationToken ct = default);

    Task<PagedResponse<HistoryTransactionResponse>> ListAsync(
        DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default);
}
