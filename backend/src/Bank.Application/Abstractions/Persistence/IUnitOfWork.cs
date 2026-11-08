namespace Bank.Application.Abstractions.Persistence;

/// <summary>
/// Groups several repository calls into one database transaction. Repositories that take part in a unit of
/// work see the open transaction automatically; there is nothing to pass around.
/// </summary>
public interface IUnitOfWork
{
    Task<ITransactionScope> BeginAsync(CancellationToken ct);

    /// <summary>
    /// Whether the failure was the database choosing this transaction as a deadlock victim. The database has
    /// already rolled it back, so running the whole operation again is safe.
    /// </summary>
    bool IsRetryable(Exception exception);
}

/// <summary>An open transaction. Disposing it without <see cref="CommitAsync"/> rolls everything back.</summary>
public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
