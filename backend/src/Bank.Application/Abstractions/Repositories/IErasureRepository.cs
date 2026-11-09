namespace Bank.Application.Abstractions.Repositories;

public interface IErasureRepository
{
    /// <summary>
    /// Deletes the user's personal finance data with its history, ends every session, closes accounts and cards and
    /// strips the person from the user record, all in one database transaction.
    /// </summary>
    Task EraseAsync(long userId, DateTime now, CancellationToken ct = default);

    /// <summary>Whether the user has been erased; a token issued earlier must stop working then.</summary>
    Task<bool> IsErasedAsync(long userId, CancellationToken ct = default);
}
