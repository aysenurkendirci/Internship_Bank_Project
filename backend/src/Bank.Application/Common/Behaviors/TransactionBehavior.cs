using Bank.Application.Abstractions.Persistence;
using MediatR;

namespace Bank.Application.Common.Behaviors;

/// <summary>
/// Marks a command whose handler must run inside a single database transaction: either every change it makes
/// is stored, or none is.
/// </summary>
public interface ITransactionalCommand;

/// <summary>
/// Opens a transaction around commands marked <see cref="ITransactionalCommand"/> and commits it only when the
/// handler finishes without an exception. Any exception (a broken business rule, a lost race, a timeout)
/// leaves the transaction uncommitted, and disposing it rolls everything back.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Checked here rather than with a generic constraint, which the container would not filter on.
        if (request is not ITransactionalCommand)
            return await next(ct);

        await using var transaction = await _unitOfWork.BeginAsync(ct);
        var response = await next(ct);
        await transaction.CommitAsync(ct);

        return response;
    }
}
