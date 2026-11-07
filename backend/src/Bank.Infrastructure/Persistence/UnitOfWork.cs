using Bank.Application.Abstractions.Persistence;
using Microsoft.Data.SqlClient;

namespace Bank.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DbSession _session;

    public UnitOfWork(DbSession session) => _session = session;

    public async Task<ITransactionScope> BeginAsync(CancellationToken ct)
        => new Scope(_session, await _session.BeginTransactionAsync(ct));

    private sealed class Scope : ITransactionScope
    {
        private readonly DbSession _session;
        private readonly SqlTransaction _transaction;
        private bool _committed;

        public Scope(DbSession session, SqlTransaction transaction)
        {
            _session = session;
            _transaction = transaction;
        }

        public async Task CommitAsync(CancellationToken ct)
        {
            await _transaction.CommitAsync(ct);
            _committed = true;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (!_committed && _transaction.Connection is not null)
                    await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _session.EndTransaction();
            }
        }
    }
}
